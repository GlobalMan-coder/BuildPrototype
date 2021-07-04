using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MessageManager : MonoBehaviour
{
    [SerializeField] Transform MessagePan;
    [SerializeField] Transform MessagePrefab;
    public static MessageManager Instance;
    private Queue<Message> Messages = new Queue<Message>();
    private bool isEmpty = true;
    private void Awake()
    {
        if (!Instance)
        {
            Instance = this;
        }
    }
    public void AddMessage(string message, Color color)
    {
        if (isEmpty) isEmpty = true;
        //Messages.Enqueue(message);
    }
    IEnumerator MessageAdding()
    {
        if (Messages.Count == 0) yield return null;
        iTween.MoveBy(MessagePan.gameObject, iTween.Hash("y", "50", "time", "1"));
        yield return new WaitForSeconds(0.5f);
        Transform m = Instantiate(MessagePrefab, MessagePan.parent);
        Message message = Messages.Dequeue();
        m.GetComponent<Image>().color = message.color;
        m.GetChild(0).GetComponent<Text>().text = message.message;
        iTween.ScaleBy(m.gameObject, iTween.Hash("y", "1", "time", "0.5"));
        yield return new WaitForSeconds(0.5f);
        
        m.parent = MessagePan;
        yield return null;
    }
    IEnumerator MessageDeleting()
    {
        return null;
    }

    public struct Message
    {
        public string message;
        public Color color;
    }
}
