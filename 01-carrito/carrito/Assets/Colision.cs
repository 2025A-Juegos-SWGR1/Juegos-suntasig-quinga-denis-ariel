using System;
using UnityEngine;

public class Colision : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D other)
    {
        Debug.Log("Ouch");
    }

    [SerializeField] private float delayDestruccion = 0.5f;
    [SerializeField] Color32 colorTienePaquete = new Color32(1, 1, 1, 1);
    [SerializeField] Color32 colorNoTienePaquete = new Color32(1, 1, 1, 1);
    bool tienePaquete;
    SpriteRenderer sprite;
    private void Start()
    {
        sprite = GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("QUE?");
        if (other.tag == "paquete" && !tienePaquete)
        {
            Debug.Log("Recoger paquete");
            tienePaquete = true;
            sprite.color = colorTienePaquete;
            Destroy(other.gameObject, delayDestruccion);
        }
        if (other.tag == "cliente" && tienePaquete)
        {
            Debug.Log("Dejar paquete");
            sprite.color = colorNoTienePaquete;
            tienePaquete = false;
        }
    }
}