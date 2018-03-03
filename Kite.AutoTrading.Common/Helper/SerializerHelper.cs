using MsgPack.Serialization;
using System;
using System.IO;
using System.Threading.Tasks;
using Trady.Core;

namespace Kite.AutoTrading.Common.Helper
{
    public static class SerializerHelper
    {
        public async static Task<bool> Serialize<T>(T thisObj, string filePath)
        {
            try
            {
                var serializer = MessagePackSerializer.Get<T>();
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await serializer.PackAsync(fileStream, thisObj);
                }
                return true;
            }
            catch (Exception ex)
            { }
            return false;
        }

        public async static Task<T> Deserialize<T>(string filePath)
        {
            if (File.Exists(filePath))
            {
                var serializer = MessagePackSerializer.Get<T>();
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    return await serializer.UnpackAsync(fileStream);
                }
            }
            return default(T);
        }
    }
}
