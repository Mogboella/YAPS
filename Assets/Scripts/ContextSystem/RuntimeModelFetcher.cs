using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using GLTFast; // Ensure glTFast is installed via Package Manager

namespace YAPS.ContextSystem
{
    [Serializable]
    public class PolyPizzaResult
    {
        public string ID;
        public string Title;
        public string Download;
    }

    [Serializable]
    public class PolyPizzaSearchResponse
    {
        public PolyPizzaResult[] results;
    }

    public class RuntimeModelFetcher : MonoBehaviour
    {
        [Header("API Credentials")]
        [Tooltip("Poly.pizza API Key")]
        public string polyPizzaAPIKey = "77b4decb0cf34130ae0970dbe3ec090c"; 

        private const string SEARCH_URL = "https://api.poly.pizza/v1/search/";

        /// <summary>
        /// Attempts to search Poly.pizza for a keyword, downloads the first GLB model, 
        /// and instantiates it into the provided container.
        /// </summary>
        public void FetchAndLoadModel(string keyword, Transform container, Action<bool> onComplete)
        {
            if (string.IsNullOrEmpty(polyPizzaAPIKey))
            {
                Debug.LogWarning("[RuntimeModelFetcher] No API key provided for Poly.pizza.");
                onComplete?.Invoke(false);
                return;
            }

            StartCoroutine(SearchAndDownloadRoutine(keyword, container, onComplete));
        }

        private IEnumerator SearchAndDownloadRoutine(string keyword, Transform container, Action<bool> onComplete)
        {
            string url = SEARCH_URL + UnityWebRequest.EscapeURL(keyword);
            Debug.Log($"[RuntimeModelFetcher] Searching PolyPizza for '{keyword}'...");

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("X-Auth-Token", polyPizzaAPIKey);
                request.timeout = 5; // Do not hang if the connection drops!

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[RuntimeModelFetcher] Search failed: {request.error}");
                    onComplete?.Invoke(false);
                    yield break;
                }

                string json = request.downloadHandler.text;
                PolyPizzaSearchResponse response = null;

                try
                {
                    response = JsonUtility.FromJson<PolyPizzaSearchResponse>(json);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[RuntimeModelFetcher] JSON Parse error: {e.Message}");
                }

                if (response != null && response.results != null && response.results.Length > 0)
                {
                    // Loop through the top few results to find one under 5MB
                    string validDownloadUrl = null;
                    string validTitle = null;
                    long maxSize = 5 * 1024 * 1024; // 5 MB LIMIT!

                    int maxChecks = Mathf.Min(5, response.results.Length);
                    for (int i = 0; i < maxChecks; i++)
                    {
                        string downloadUrl = response.results[i].Download;
                        
                        // Check file size using a lightweight HEAD request
                        using (UnityWebRequest headReq = UnityWebRequest.Head(downloadUrl))
                        {
                            headReq.timeout = 3;
                            yield return headReq.SendWebRequest();

                            if (headReq.result == UnityWebRequest.Result.Success)
                            {
                                string sizeStr = headReq.GetResponseHeader("Content-Length");
                                if (long.TryParse(sizeStr, out long sizeBytes))
                                {
                                    if (sizeBytes <= maxSize)
                                    {
                                        validDownloadUrl = downloadUrl;
                                        validTitle = response.results[i].Title;
                                        break; // Found a safe model!
                                    }
                                    else
                                    {
                                        Debug.LogWarning($"[RuntimeModelFetcher] Skipping '{response.results[i].Title}' ({sizeBytes / 1024 / 1024f:F1}MB) - Over 5MB limit.");
                                    }
                                }
                                else
                                {
                                    // If Content-Length is missing, skip it to be safe.
                                    Debug.LogWarning($"[RuntimeModelFetcher] Skipping '{response.results[i].Title}' - Unknown file size.");
                                }
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(validDownloadUrl))
                    {
                        Debug.Log($"[RuntimeModelFetcher] Found safe model '{validTitle}'. Downloading GLB strictly over UnityWebRequest...");
                        
                        using (UnityWebRequest dlReq = UnityWebRequest.Get(validDownloadUrl))
                        {
                            dlReq.timeout = 10;
                            yield return dlReq.SendWebRequest();

                            if (dlReq.result == UnityWebRequest.Result.Success)
                            {
                                Debug.Log($"[RuntimeModelFetcher] Download complete. Loading raw memory directly into pure main-thread glTFast...");
                                byte[] rawGLB = dlReq.downloadHandler.data;
                                LoadGltfBytesSafeSync(rawGLB, container, onComplete);
                            }
                            else
                            {
                                Debug.LogError($"[RuntimeModelFetcher] Download failed: {dlReq.error}");
                                onComplete?.Invoke(false);
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError($"[RuntimeModelFetcher] No models under 5MB found on Poly.pizza for '{keyword}'.");
                        onComplete?.Invoke(false);
                    }
                }
                else
                {
                    Debug.Log($"[RuntimeModelFetcher] No models found on Poly.pizza for '{keyword}'.");
                    onComplete?.Invoke(false);
                }
            }
        }

        private async void LoadGltfBytesSafeSync(byte[] glbData, Transform container, Action<bool> onComplete)
        {
            try
            {
                // UninterruptedDeferAgent forces ALL parsing and instantiation to run immediately synchronously!
                // This permanently bypasses Unity's broken async/await thread-hopping behavior in Play Mode.
                var deferAgent = new GLTFast.UninterruptedDeferAgent();
                var gltf = new GltfImport(deferAgent: deferAgent);
                
                bool success = await gltf.Load(glbData);

                if (success)
                {
                    if (container == null) 
                    {
                        onComplete?.Invoke(false);
                        return;
                    }

                    bool instanced = await gltf.InstantiateMainSceneAsync(container);
                    if (instanced)
                    {
                        Debug.Log("[RuntimeModelFetcher] Successfully spawned 3D Model synchronously!");
                        NormalizeScale(container);
                        onComplete?.Invoke(true);
                    }
                    else
                    {
                        Debug.LogError("[RuntimeModelFetcher] Model loaded but failed to instantiate. Missing material generators or unsupported Draco compression?");
                        onComplete?.Invoke(false);
                    }
                }
                else
                {
                    Debug.LogError("[RuntimeModelFetcher] Failed to parse GLB bytes! Ensure Draco compression is NOT used (or install com.unity.cloud.draco).");
                    onComplete?.Invoke(false);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RuntimeModelFetcher] GLTF Fatal Exception: {ex.Message}\n{ex.StackTrace}");
                onComplete?.Invoke(false);
            }
        }
        
        private void NormalizeScale(Transform container)
        {
            // Simple bound calculation to shrink massive models to ~0.5 units
            Renderer[] renderers = container.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            foreach (var r in renderers) { bounds.Encapsulate(r.bounds); }

            float maxExtent = bounds.size.magnitude;
            if (maxExtent > 0)
            {
                float targetSize = 0.5f; // Keep the target scale aligned with procedural props
                float scaleFactor = targetSize / maxExtent;
                // Apply a uniform scale
                container.localScale = Vector3.one * scaleFactor;
                // Move it up slightly so it sits on the table/floor
                container.localPosition = new Vector3(0, bounds.extents.y * scaleFactor, 0);
            }
        }
    }
}