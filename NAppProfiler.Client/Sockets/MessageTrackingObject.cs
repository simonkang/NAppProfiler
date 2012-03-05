using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NAppProfiler.Client.Sockets
{
    class MessageTrackingObject
    {
        public Guid MessageGuid { get; private set; }
        public DateTime Created { get; private set; }
        public object MessageBag { get; private set; }

        public MessageTrackingObject(object messageBag)
        {
            this.MessageGuid = Guid.NewGuid();
            this.MessageBag = messageBag;
            this.Created = DateTime.UtcNow;
        }
    }
}
