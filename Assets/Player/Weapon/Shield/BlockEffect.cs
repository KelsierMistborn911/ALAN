using UnityEngine;
using UnityEngine.Events;

public class BlockEffect : MonoBehaviour
{
    public UnityEvent OnBlock;   // назначь в инспекторе нужную реакцию

    public void TriggerBlock()
    {
        OnBlock?.Invoke();
    }

    private void Start()
    {
        // Например, автоматически запустить частицы
        var ps = GetComponent<ParticleSystem>();
        if (ps != null) ps.Play();
    }
}
