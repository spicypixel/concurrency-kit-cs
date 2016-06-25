/*

Author: Aaron Oneal, http://aarononeal.info

Copyright (c) 2012 Spicy Pixel, http://spicypixel.com

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

*/
using UnityEngine;
using System.Collections;
using System.Threading.Tasks;
using SpicyPixel.Threading;
using SpicyPixel.Threading.Tasks;
using System;

namespace SpicyPixel.Threading
{
	class SharedConcurrentBehaviour : ConcurrentBehaviour
	{
		static ConcurrentBehaviour sharedInstance;

		internal static new ConcurrentBehaviour SharedInstance {
			get {
				if(sharedInstance == null)
					sharedInstance = CreateSharedInstance();
				return sharedInstance;
			}
		}

		static ConcurrentBehaviour CreateSharedInstance() {
			GameObject go = new GameObject("Shared Concurrency Kit Scheduler");
            if (!Application.isEditor)
			    DontDestroyOnLoad(go);
			go.hideFlags = HideFlags.HideAndDontSave;
			var sharedInstance = go.AddComponent<SharedConcurrentBehaviour>();

			AppDomain.CurrentDomain.DomainUnload += (sender, e) => {
				if(sharedInstance != null)
					sharedInstance.OnDomainUnload();
			};

			return sharedInstance;
		}

		//void OnApplicationQuit()
		void OnDomainUnload()
		{
			if(gameObject != null) {
				GameObject.DestroyImmediate(gameObject);
			}
			sharedInstance = null;
		}
	}
}