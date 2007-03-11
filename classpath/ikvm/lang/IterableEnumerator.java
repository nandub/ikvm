/*
  Copyright (C) 2007 Jeroen Frijters

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
package ikvm.lang;

import cli.System.Collections.IEnumerator;
import cli.System.InvalidOperationException;
import java.util.Iterator;

public final class IterableEnumerator implements IEnumerator
{
    private static final Object UNDEFINED = new Object();
    private final Iterable src;
    private Iterator iter;
    private Object current;

    public IterableEnumerator(Iterable src)
    {
        this.src = src;
        Reset();
    }

    public boolean MoveNext()
    {
        if (iter.hasNext())
        {
            current = iter.next();
            return true;
        }
        current = UNDEFINED;
        return false;
    }

    public Object get_Current()
    {
        if (current == UNDEFINED)
        {
            // HACK we abuse Thread.stop() to throw a checked exception
            Thread.currentThread().stop(new InvalidOperationException());
        }
        return current;
    }

    public void Reset()
    {
        iter = src.iterator();
        current = UNDEFINED;
    }
}
