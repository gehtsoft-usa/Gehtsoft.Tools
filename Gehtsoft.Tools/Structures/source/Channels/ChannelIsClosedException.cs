using System;

namespace Gehtsoft.Tools.Structures.Channels
{
    public class ChannelIsClosedException : Exception
    {
        public ChannelIsClosedException() : base("Channel is closed")
        {

        }
    }
}