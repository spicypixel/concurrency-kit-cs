using UnityEngine;
using System.Collections;

namespace SpicyPixel
{
	public class ConcurrencyKitRotateObject : MonoBehaviour {

		// Use this for initialization
		void Start () {
		
		}
		
		// Update is called once per frame
		void Update () {
			transform.RotateAround(Vector3.up, -2f * Time.deltaTime);
		}
	}
}