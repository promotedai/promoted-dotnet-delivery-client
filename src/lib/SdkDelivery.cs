using System;
using System.Drawing;
namespace Promoted.Lib
{
    public static class SdkDelivery
    {
        public static Promoted.Delivery.Response Deliver(Promoted.Delivery.Request req,
                                                         DeliveryRequestOptions? options = null)
        {
            options ??= new DeliveryRequestOptions();

            var resp = new Promoted.Delivery.Response();

            req.RequestId = Guid.NewGuid().ToString();
            resp.RequestId = req.RequestId;

            Promoted.Delivery.Paging paging = req.Paging;
            if (paging == null)
            {
                paging = new Promoted.Delivery.Paging { Offset = 0, Size = req.Insertion.Count };
            }
            // Alter offset and size if necessary.
            int offset = Math.Max(0, paging.Offset);
            if (offset < options.RetrievalInsertionOffset)
            {
                throw new ArgumentException("Paging offset must be >= RetrievalInsertionOffset.");
            }
            int size = paging.Size;
            if (size <= 0)
            {
                size = req.Insertion.Count;
            }
            int relativeOffset = offset - options.RetrievalInsertionOffset;
            int finalSize = Math.Min(size, req.Insertion.Count - relativeOffset);

            for (int i = 0; i < finalSize; ++i)
            {
                var insertion = new Promoted.Delivery.Insertion();
                insertion.Position = (ulong)offset++;
                insertion.ContentId = req.Insertion[relativeOffset++].ContentId;
                insertion.InsertionId = Guid.NewGuid().ToString();
                resp.Insertion.Add(insertion);
            }

            return resp;
        }
    }
}
