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
    [SerializeField] Color SuccessColor = new Color(0.2f, 0.8f, 0.3f, 0.5f);
    [SerializeField] Color NotifyColor = new Color(0.8f, 0.8f, 0.3f, 0.5f);
    [SerializeField] Color AlertColor = new Color(1f, 0.2f, 0.2f, 0.5f);

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

    public void AddMessage(string message, Type type)
    {
        Messages.Enqueue(new Message { message = message, color = GetColor(type)});
        if (!exist)
        {
            exist = true;
            MessageAdding();
        }
    }

    private Color GetColor(Type type)
    {
        switch (type)
        {
            case Type.Alert:
                return AlertColor;
            case Type.Notify:
                return NotifyColor;
            case Type.Success:
            default:
                return SuccessColor;
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
            m.SetParent(MessagePan);
            MessageAdding();
            Destroy(m.gameObject, displayTime);
        });
    }

    public struct Message
    {
        public string message;
        public Color color;
    }

    public enum Type
    {
        Success,
        Notify,
        Alert,
    }
}
