using UnityEngine;

public class Rotater : MonoBehaviour
{
    [SerializeField] private float _speed = 10f;
    
    private void Update()
    {
        float t = Time.deltaTime * _speed;
        transform.Rotate(t, t, t);
    }
}
