using UnityEngine;

public class Camara : MonoBehaviour
{
    [SerializeField] GameObject cosaALaQueSeguir; 
    // Update is called once per frame

    void LateUpdate()
    {

            transform.position = cosaALaQueSeguir.transform.position + new Vector3(0, 0, -10);
    }
}
