using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TodoManager : MonoBehaviour
{
    public TMP_InputField inputField;
    public Transform contentParent;
    public GameObject todoItemPrefab; // a prefab with Toggle + TMP_Text

    public void AddTask()
    {
        if (string.IsNullOrEmpty(inputField.text)) return;
        var item = Instantiate(todoItemPrefab, contentParent);
        item.GetComponentInChildren<TMP_Text>().text = inputField.text;
        inputField.text = "";
    }
}
