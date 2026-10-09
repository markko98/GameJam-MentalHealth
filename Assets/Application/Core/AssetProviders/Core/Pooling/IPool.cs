using UnityEngine;

public interface IPool
{
    void ReturnToPool(GameObject objectToReturn, GameObject key);
}
