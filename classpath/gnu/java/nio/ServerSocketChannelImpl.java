/*
  Copyright (C) 2002, 2003, 2004, 2005, 2006 Jeroen Frijters

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
package gnu.java.nio;

import gnu.java.net.PlainSocketImpl;
import java.io.IOException;
import java.net.NioServerSocket;
import java.net.ServerSocket;
import java.net.SocketTimeoutException;
import java.nio.channels.ClosedChannelException;
import java.nio.channels.NotYetBoundException;
import java.nio.channels.ServerSocketChannel;
import java.nio.channels.SocketChannel;
import java.nio.channels.spi.SelectorProvider;

public final class ServerSocketChannelImpl extends ServerSocketChannel
{
    private NioServerSocket socket;
    private boolean blocking = true;

    protected ServerSocketChannelImpl(SelectorProvider provider) throws IOException
    {
        super(provider);
        socket = new NioServerSocket(this);
    }

    protected void implCloseSelectableChannel() throws IOException
    {
        socket.close();
    }

    protected void implConfigureBlocking(boolean blocking) throws IOException
    {
        this.blocking = blocking;
    }

    public SocketChannel accept() throws IOException
    {
        if (!isOpen())
            throw new ClosedChannelException();

        if (!socket.isBound())
            throw new NotYetBoundException();

        boolean completed = false;
    
        try
        {
            begin();
            if (blocking || socket.pollAccept())
            {
                PlainSocketImpl impl = new PlainSocketImpl();
                socket.accept(impl);
                completed = true;
                return new SocketChannelImpl(provider(), impl, true);
            }
            else
            {
                return null;
            }
        }
        finally
        {
            end(completed);
        }
    }

    public ServerSocket socket()
    {
        return socket;
    }

    cli.System.Net.Sockets.Socket getSocket()
    {
        return socket.getSocket();
    }
}
