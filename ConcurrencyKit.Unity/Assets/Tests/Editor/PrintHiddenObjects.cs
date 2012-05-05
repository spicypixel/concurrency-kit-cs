using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;


public class PrintHiddenObjects : ScriptableObject
{
	[MenuItem( "GameObject/Print Hidden Objects" )]
    static void PrintHiddenObjectsCommand()
	{
		if(!Application.isPlaying)
			return;
		
		string output = "Hidden Objects:\n";
		
		var o = Resources.FindObjectsOfTypeAll(typeof(Object));
		foreach(var item in o)
		{
			// Is a prefab
			if(PrefabUtility.GetPrefabParent(item) == null && PrefabUtility.GetPrefabObject(item) != null)
				continue;
						
			if ((item.hideFlags & HideFlags.HideInHierarchy) == HideFlags.HideInHierarchy)
			{
				output += "<" + item.GetType() + ">: " + item.name + "\n";
				
                //item.hideFlags = item.hideFlags ^ HideFlags.HideInHierarchy;
                //item.active = !item.active;
                //item.active = !item.active;
			}
			else 
			{
				//Debug.Log("Skipping: " + item.name);
			}
		}
		
		Debug.Log(output);
	}
	
	[MenuItem( "GameObject/Print Hidden GameObjects" )]
    static void PrintHiddenGameObjectsCommand()
	{
		if(!Application.isPlaying)
			return;
		
		string output = "Hidden GameObjects:\n";
		
		var o = Resources.FindObjectsOfTypeAll(typeof(GameObject));
		foreach(var item in o)
		{
			// Is a prefab
			if(PrefabUtility.GetPrefabParent(item) == null && PrefabUtility.GetPrefabObject(item) != null)
				continue;
						
			if ((item.hideFlags & HideFlags.HideInHierarchy) == HideFlags.HideInHierarchy)
			{
				output += "<" + item.GetType() + ">: " + item.name + "\n";
				//output += item.name + "\n";
				
                //item.hideFlags = item.hideFlags ^ HideFlags.HideInHierarchy;
                //item.active = !item.active;
                //item.active = !item.active;
			}
			else 
			{
				//Debug.Log("Skipping: " + item.name);
			}
		}
		
		Debug.Log(output);
	}
}