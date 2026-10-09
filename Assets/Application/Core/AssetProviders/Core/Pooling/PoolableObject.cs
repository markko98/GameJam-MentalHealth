using System;
using UnityEngine;

public class PoolableObject : MonoBehaviour
{
    private IPool pool;
    private GameObject key;

    public void SetPool(GameObject key, IPool pool)
    {
        this.key = key;
        this.pool = pool;
    }

    public void ReturnToPool()
    {
        pool.ReturnToPool(gameObject, key);
    }
}
