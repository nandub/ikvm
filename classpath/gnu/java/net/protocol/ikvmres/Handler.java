/*
  Copyright (C) 2002 Jeroen Frijters

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

package gnu.java.net.protocol.ikvmres;

import system.io.*;
import system.reflection.*;
import java.net.*;
import java.io.*;
import java.io.IOException;

class IkvmresURLConnection extends URLConnection
{
	private InputStream inputStream;

	IkvmresURLConnection(URL url)
	{
		super(url);
		doOutput = false;
	}

	public void connect() throws IOException
	{
		if(!connected)
		{
			String assembly = url.getHost();
			String resource = url.getFile();
			// TODO error handling
			FieldInfo fi = Assembly.Load(assembly).GetLoadedModules()[0].GetField(resource);
			byte[] b = new byte[system.runtime.interopservices.Marshal.SizeOf(fi.get_FieldType())];
			InitArray(b, fi);
			inputStream = new ByteArrayInputStream(b);
			connected = true;
		}
	}
	private static native void InitArray(byte[] buf, FieldInfo field);

	public InputStream getInputStream() throws IOException
	{
		if(!connected)
		{
			connect();
		}
		return inputStream;
	}

	public OutputStream getOutputStream() throws IOException
	{
		throw new IOException("resource URLs are read only");
	}

	public long getLastModified()
	{
		return -1;
	}

	public int getContentLength()
	{
		return -1;
	}
}

public class Handler extends URLStreamHandler
{
	protected URLConnection openConnection(URL url) throws IOException
	{
		return new IkvmresURLConnection(url);
	}

	protected void parseURL(URL url, String url_string, int start, int end)
	{
		int colon = url_string.indexOf(':', start);
		String file = url_string.substring(start, colon);
		String assembly = url_string.substring(colon + 1);
		setURL(url, "ikvmres", assembly, 0, file, null);
	}

	protected String toExternalForm(URL url)
	{
		return "ikvmres:" + url.getFile() + ":" + url.getHost();
	}
}
