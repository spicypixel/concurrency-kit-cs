using System;

namespace System.Threading
{
	/// <summary>
	/// Interlocked reference exchanges do not work with the older
	/// Mono AOT compiler. This type fudges around it using a spinlock,
	/// which works (probably more slowly) so long as all exchanges are performed
	/// through this mechanism.
	/// 
	/// See AOT compiler fixes: 79b99d358b1321759ea3a42f22eaf141aecf0e79 on the mono 2.10 branch
	/// http://permalink.gmane.org/gmane.comp.gnome.mono.patches/182067
	/// https://bugzilla.xamarin.com/show_bug.cgi?id=234
	/// </summary>
    public static class AotInterlocked
    {
		static SpinLock spinLock = new SpinLock(false);

		public static T Exchange<T>(ref T target, T newValue)
		{
			bool lockTaken = false;
			try
			{
				spinLock.Enter(ref lockTaken);
				
				T originalValue = target;
				
				target = newValue;
				
				return originalValue;
			}
			finally
			{
				if(lockTaken)
					spinLock.Exit(false);
			}
		}

		public static T CompareExchange<T>(ref T target, T newValue, T comparand)
		{
			bool lockTaken = false;
			try
			{
				spinLock.Enter(ref lockTaken);
				
				T originalValue = target;
				
				if(Object.ReferenceEquals(target, comparand))
					target = newValue;
				
				return originalValue;
			}
			finally
			{
				if(lockTaken)
					spinLock.Exit(false);
			}
		}
    }
}

