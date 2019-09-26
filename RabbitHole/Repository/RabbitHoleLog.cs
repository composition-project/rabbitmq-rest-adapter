using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RabbitHole.Repository
{
    public class RabbitHoleLog
    {
        public int id { get; set; }
        public string Method { get; set; }
        public string Request { get; set; }
        public string ContentType { get; set; }
        public string PicasoHeader { get; set; }
        public string ODS { get; set; }
        public DateTime Timestamp { get; set; }
        public double Duration { get; set; }
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public string Exception { get; set; }
    }
}
