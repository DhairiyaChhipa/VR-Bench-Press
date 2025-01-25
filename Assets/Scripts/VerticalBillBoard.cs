using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VerticalBillBoard : MonoBehaviour
{
    public Transform target;

    void Update()
    {
        transform.LookAt(target, Vector3.up);
    }
}
