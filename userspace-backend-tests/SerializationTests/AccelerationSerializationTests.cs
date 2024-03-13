using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using userspace_backend.Data.Profiles;
using userspace_backend.Data.Profiles.Accel.Formula;
using userspace_backend.IO.Serialization;

namespace userspace_backend_tests.SerializationTests
{
    [TestClass]
    public class AccelerationSerializationTests
    {
        protected class AccelerationOnlyObject
        {
            [JsonConverter(typeof(AccelerationJsonConverter))]
            public Acceleration Acceleration { get; set; }
        }

        [TestMethod]
        public void DeserializeNoAccel()
        {
            string textToDeserialize = """
                {
                    "Acceleration": {
                        "Type": "None"
                    }
                }
                """;

            var deserializedText = JsonSerializer.Deserialize<AccelerationOnlyObject>(textToDeserialize);
            Assert.AreEqual(Acceleration.AccelerationDefinitionType.None, deserializedText.Acceleration.Type);
        }

        [TestMethod]
        public void DeserializeFormulaClassicAccel()
        {
            string textToDeserialize = """
                {
                    "Acceleration": {
                        "Type": "Formula/Classic",
                        "Acceleration": 0.001,
                        "Exponent": 2
                    }
                }
                """;

            var deserializedText = JsonSerializer.Deserialize<AccelerationOnlyObject>(textToDeserialize);
            Assert.AreEqual(Acceleration.AccelerationDefinitionType.Formula, deserializedText.Acceleration.Type);
            var actualClassicAccel = deserializedText.Acceleration as ClassicAccel;
            Assert.IsNotNull(actualClassicAccel);
            Assert.AreEqual(0.001, actualClassicAccel.Acceleration);
            Assert.AreEqual(2, actualClassicAccel.Exponent);
        }

        [TestMethod]
        public void DeserializeFormulaLinearAccel()
        {
            string textToDeserialize = """
                {
                    "Acceleration": {
                        "Type": "Formula/Linear",
                        "Acceleration": 0.001
                    }
                }
                """;

            var deserializedText = JsonSerializer.Deserialize<AccelerationOnlyObject>(textToDeserialize);
            Assert.AreEqual(Acceleration.AccelerationDefinitionType.Formula, deserializedText.Acceleration.Type);
            var actualLinearAccel = deserializedText.Acceleration as LinearAccel;
            Assert.IsNotNull(actualLinearAccel);
            Assert.AreEqual(0.001, actualLinearAccel.Acceleration);
        }
    }
}
