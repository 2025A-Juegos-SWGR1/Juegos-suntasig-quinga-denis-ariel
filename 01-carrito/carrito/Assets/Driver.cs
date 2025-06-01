using UnityEngine;

public class Driver : MonoBehaviour
{
    [SerializeField] private float velocidadGiro = 85f;
    [SerializeField] private float velocidadMovimiento = 6f;
    [SerializeField] private float velocidadGiroRapido = 105f;
    [SerializeField] private float velocidadMovimientoRapido = 10f;
    [SerializeField] private float velocidadGiroLento = 85f;
    [SerializeField] private float velocidadMovimientoLento = 4f;
    // Update is called once per frame
    void Update()
    {
        float valorGiro = Input.GetAxis("Horizontal") * velocidadGiro * Time.deltaTime;
        float valorMovimiento = Input.GetAxis("Vertical") * velocidadMovimiento * Time.deltaTime;
        transform.Rotate(0, 0, -valorGiro);
        transform.Translate(0,valorMovimiento, 0);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "velocidad")
        {
            velocidadGiro = velocidadGiroRapido;
            velocidadMovimiento = velocidadMovimientoRapido;
            Destroy(other.gameObject);
        }
        else if (other.tag == "lento")
        {
            velocidadGiro = velocidadGiroLento;
            velocidadMovimiento = velocidadMovimientoLento;
        }
    }
    
    
}