using UnityEngine;
using UnityEngine.UI;

public class UIImageViewComponent : MonoBehaviour
{
	public Image image;
	private void OnValidate()
	{
		if (image == null) TryGetComponent<Image>(out image);
	}
}
