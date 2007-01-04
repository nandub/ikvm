/*
  Copyright (C) 2006 Jeroen Frijters

  This software is provided 'as-is', without any express or implied
  warranty.  In no event will the authors be held liable for any damages
  arising from the use of this software.

  Permission is granted to anyone to use this software for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this software must not be misrepresented; you must not
     claim that you wrote the original software. If you use this software
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original software.
  3. This notice may not be removed or altered from any source distribution.

  Jeroen Frijters
  jeroen@frijters.net
  
*/
package ikvm.runtime;

import cli.System.Type;
import cli.System.RuntimeTypeHandle;

public final class Util
{
    private Util()
    {
    }

    public static native Class getClassFromObject(Object o);

    public static native Class getClassFromTypeHandle(RuntimeTypeHandle handle);

    public static native Class getFriendlyClassFromType(Type type);

    public static Type getInstanceTypeFromClass(Class classObject)
    {
        return GetInstanceTypeFromTypeWrapper(VMClass.getWrapper(classObject));
    }

    private static native Type GetInstanceTypeFromTypeWrapper(Object wrapper);

    //[HideFromJava]
    public static Throwable mapException(Throwable x)
    {
        return ExceptionHelper.MapExceptionFast(x, true);
    }
}
