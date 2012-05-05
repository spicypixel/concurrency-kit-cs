using UnityEditor;
using UnityEngine;
using System.Collections;
using SpicyPixel.Threading.Tasks;
using System.Threading.Tasks;

[ExecuteInEditMode]
public class EditorScript : MonoBehaviour {
	TaskFactory f;

	// Use this for initialization
	void Start () {
		f = UnityTaskFactory.Default;
		UnityTaskFactory.Default.StartNew(() => {
			Debug.Log("Running editor task");
		});
	}
	
	// Update is called once per frame
	void Update () {
		if(UnityTaskFactory.Default != f)
			Debug.LogError("Task Factory not the same");
	}
}
