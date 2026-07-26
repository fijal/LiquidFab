using UnityEngine;

public class Stabilizer : MonoBehaviour
{
    void Update()
    {
        transform.rotation = Quaternion.Euler(90, 0, 0);    
    }
}
