using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class MessageManager : MonoBehaviour
{
    [SerializeField] Transform MessagePan;
    [SerializeField] Transform MessagePrefab;
    [SerializeField] float appearTime = 0.3f;
    [SerializeField] float displayTime = 3f;
    public static MessageManager Instance;
    private Queue<Message> Messages = new Queue<Message>();
    private bool exist = false;
    private void Awake()
    {
        if (!Instance)
        {
            Instance = this;
        }
    }

    public void AddMessage(string message, Color color)
    {
        Messages.Enqueue(new Message { message = message, color = color });
        if (!exist)
        {
            exist = true;
            MessageAdding();
        }
    }
    void MessageAdding()
    {
        if (Messages.Count == 0)
        {
            exist = false;
            return;
        }
        MessagePan.DOMoveY(MessagePan.position.y - 50, appearTime);
        Transform m = Instantiate(MessagePrefab, MessagePan.parent);
        Message message = Messages.Dequeue();
        m.GetComponent<Image>().color = message.color;
        m.GetChild(0).GetComponent<Text>().text = message.message;
        m.DOScaleY(1, appearTime).OnComplete(() =>
        {
            m.parent = MessagePan;
            MessageAdding();
            Destroy(m.gameObject, displayTime);
        });
    }

    public struct Message
    {
        public string message;
        public Color color;
    }
}
