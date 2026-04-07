using System;
using System.Collections.Generic;
using UnityEngine;

namespace YAPS.ContextSystem
{
    [Serializable]
    public class CategoryPropMapping
    {
        public ContextCategory category;
        public GameObject prefab;
    }

    public class PropManager : MonoBehaviour
    {
        [Header("Prop Configuration")]
        [Tooltip("The default prefab (e.g. Orb) used if a category has no specific prefab mapping.")]
        public GameObject defaultOrbPrefab;
        
        [Tooltip("List mapping categories to specific prefabs.")]
        public List<CategoryPropMapping> propMappings = new List<CategoryPropMapping>();

        [Header("Spawn Settings")]
        [Tooltip("Where to spawn the prop relative to Inky.")]
        public Vector3 spawnOffset = new Vector3(0.5f, 1.0f, 0.5f);
        [Tooltip("If true, the prop will be parented to this PropManager's transform so it moves with Inky.")]
        public bool parentToManager = true;

        private GameObject currentActiveProp;

        /// <summary>
        /// Spawns a prop for the given category at the offset location.
        /// </summary>
        public GameObject SpawnProp(ContextCategory category, string keyword = "")
        {
            // Destroy existing prop if there's one
            ClearProp();

            Vector3 spawnPosition = transform.position + transform.TransformDirection(spawnOffset);

            GameObject prefabToSpawn = GetPrefabForCategory(category);
            if (prefabToSpawn != null)
            {
                // We have a custom local prefab
                currentActiveProp = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
                ParentAndLog(category);
                return currentActiveProp;
            }
            
            // Try Runtime Model Fetching
            var fetcher = GetComponent<RuntimeModelFetcher>();
            if (fetcher != null && !string.IsNullOrEmpty(keyword) && keyword != "box")
            {
                // Create container
                currentActiveProp = new GameObject($"DynamicProp_{keyword}");
                currentActiveProp.transform.position = spawnPosition;
                trackedProps.Add(currentActiveProp);
                ParentAndLog(category);

                // Background download the dynamic model
                fetcher.FetchAndLoadModel(keyword, currentActiveProp.transform, success => {
                    // Instantiation handled by RuntimeModelFetcher
                });

                return currentActiveProp;
            }

            // Automatically generate a testing prop so the system works out-of-the-box!
            currentActiveProp = CreateProceduralProp(category, spawnPosition);
            ParentAndLog(category);
            return currentActiveProp;
        }

        private void ParentAndLog(ContextCategory category)
        {
            if (parentToManager && currentActiveProp != null)
            {
                currentActiveProp.transform.SetParent(transform, true);
            }
            Debug.Log($"[PropManager] Spawned '{currentActiveProp.name}' for category: {category}");
        }

        private List<GameObject> trackedProps = new List<GameObject>();

        /// <summary>
        /// Instantiates basic 3D shapes to represent categories if no prefabs are supplied.
        /// </summary>
        private GameObject CreateProceduralProp(ContextCategory category, Vector3 position)
        {
            GameObject container = new GameObject($"ProceduralProp_{category}");
            container.transform.position = position;

            GenerateProceduralShapesInto(container, category);

            trackedProps.Add(container);
            return container;
        }

        private void GenerateProceduralShapesInto(GameObject container, ContextCategory category)
        {
            switch (category)
            {
                case ContextCategory.Fitness:
                    GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    handle.transform.SetParent(container.transform);
                    handle.transform.localScale = new Vector3(0.1f, 0.4f, 0.1f);
                    handle.transform.localPosition = Vector3.zero;

                    GameObject leftWeight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    leftWeight.transform.SetParent(container.transform);
                    leftWeight.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
                    leftWeight.transform.localPosition = new Vector3(0, -0.4f, 0);
                    leftWeight.GetComponent<Renderer>().material.color = Color.black;

                    GameObject rightWeight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    rightWeight.transform.SetParent(container.transform);
                    rightWeight.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
                    rightWeight.transform.localPosition = new Vector3(0, 0.4f, 0);
                    rightWeight.GetComponent<Renderer>().material.color = Color.black;
                    break;
                
                case ContextCategory.Study:
                    GameObject book = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    book.transform.SetParent(container.transform);
                    book.transform.localScale = new Vector3(0.4f, 0.1f, 0.6f);
                    book.transform.localPosition = Vector3.zero;
                    book.GetComponent<Renderer>().material.color = new Color(0.8f, 0.2f, 0.2f); // Red book
                    break;

                case ContextCategory.Work:
                    GameObject briefcase = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    briefcase.transform.SetParent(container.transform);
                    briefcase.transform.localScale = new Vector3(0.6f, 0.4f, 0.2f);
                    briefcase.transform.localPosition = Vector3.zero;
                    briefcase.GetComponent<Renderer>().material.color = new Color(0.4f, 0.2f, 0.1f); // Brown
                    break;

                case ContextCategory.Sports:
                    GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    ball.transform.SetParent(container.transform);
                    ball.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                    ball.transform.localPosition = Vector3.zero;
                    ball.GetComponent<Renderer>().material.color = new Color(1f, 0.5f, 0f); // Orange basketball
                    break;

                case ContextCategory.Social:
                    // Two small spheres chatting
                    GameObject p1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    p1.transform.SetParent(container.transform); p1.transform.localScale = Vector3.one * 0.3f;
                    p1.transform.localPosition = new Vector3(-0.2f, 0, 0); p1.GetComponent<Renderer>().material.color = Color.magenta;
                    
                    GameObject p2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    p2.transform.SetParent(container.transform); p2.transform.localScale = Vector3.one * 0.3f;
                    p2.transform.localPosition = new Vector3(0.2f, 0, 0); p2.GetComponent<Renderer>().material.color = Color.yellow;
                    break;

                case ContextCategory.Leisure:
                    // Couch or simple TV (flat rectangle)
                    GameObject tv = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tv.transform.SetParent(container.transform);
                    tv.transform.localScale = new Vector3(0.8f, 0.5f, 0.05f);
                    tv.transform.localPosition = Vector3.zero;
                    tv.GetComponent<Renderer>().material.color = Color.black;
                    break;

                default:
                    // Default Orb
                    GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    orb.transform.SetParent(container.transform);
                    orb.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
                    orb.transform.localPosition = Vector3.zero;
                    orb.GetComponent<Renderer>().material.color = Color.cyan;
                    break;
            }
        }

        public void ClearProp()
        {
            if (currentActiveProp != null)
            {
                SafeDestroy(currentActiveProp);
                currentActiveProp = null;
            }

            // Absolute bulletproof cleanup tracking
            foreach (GameObject prop in trackedProps)
            {
                if (prop != null)
                {
                    SafeDestroy(prop);
                }
            }
            trackedProps.Clear();

            // Native scene cleanup just in case
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.StartsWith("ProceduralProp") || (obj.name.Contains("(Clone)") && obj.transform.parent == transform))
                {
                    SafeDestroy(obj);
                }
            }
        }

        private void SafeDestroy(GameObject obj)
        {
            if (Application.isPlaying)
                Destroy(obj);
            else
                DestroyImmediate(obj);
        }

        /// <summary>
        /// Retrieve the correct prefab for a category, or the default.
        /// </summary>
        private GameObject GetPrefabForCategory(ContextCategory category)
        {
            foreach (var mapping in propMappings)
            {
                if (mapping.category == category && mapping.prefab != null)
                {
                    return mapping.prefab;
                }
            }
            return defaultOrbPrefab;
        }

        /// <summary>
        /// Gets the current active prop for manipulation (like rotating towards camera)
        /// </summary>
        public GameObject GetCurrentProp()
        {
            return currentActiveProp;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0, 1, 0, 0.5f);
            Vector3 spawnPos = transform.position + transform.TransformDirection(spawnOffset);
            Gizmos.DrawSphere(spawnPos, 0.1f);
            Gizmos.DrawLine(transform.position, spawnPos);
        }
    }
}
