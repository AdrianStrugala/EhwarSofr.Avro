#region license
/**Copyright (c) 2021 Adrian Struga�a
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* https://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/
#endregion

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SolTechnology.Avro.Attributes;
using SolTechnology.Avro.BuildSchema.SchemaModels.Abstract;

namespace SolTechnology.Avro.BuildSchema.SchemaModels
{

    internal sealed class DurationSchema : LogicalTypeSchema
    {
        public DurationSchema(Type runtimeType) : base(runtimeType)
        {
            BaseTypeSchema = new FixedSchema(
                new NamedEntityAttributes(new SchemaName("duration"), new List<string>(), ""),
                12,
                typeof(TimeSpan));
        }

        internal override Avro.Schema.Schema.Type Type => Avro.Schema.Schema.Type.Logical;
        internal override TypeSchema BaseTypeSchema { get; set; }
        internal override string LogicalTypeName => "duration";

        internal override void ToJsonSafe(JsonTextWriter writer, HashSet<NamedSchema> seenSchemas)
        {
            var baseSchema = (FixedSchema) BaseTypeSchema;
            writer.WriteStartObject();
            writer.WriteProperty("type", baseSchema.Type.ToString().ToLowerInvariant());
            writer.WriteProperty("size", baseSchema.Size);
            writer.WriteProperty("name", baseSchema.Name);
            writer.WriteProperty("logicalType", LogicalTypeName);
            writer.WriteEndObject();
        }

        internal object ConvertToBaseValue(object logicalValue, DurationSchema schema)
        {
            var duration = (TimeSpan)logicalValue;

            byte[] bytes = new byte[16];
            var monthsBytes = BitConverter.GetBytes(0);
            var daysBytes = BitConverter.GetBytes(duration.Days);

            var milliseconds = ((duration.Hours * 60 + duration.Minutes) * 60 + duration.Seconds) * 1000 + duration.Milliseconds;
            var millisecondsBytes = BitConverter.GetBytes(milliseconds);


            System.Array.Copy(monthsBytes, 0, bytes, 0, 4);
            System.Array.Copy(daysBytes, 0, bytes, 4, 4);
            System.Array.Copy(millisecondsBytes, 0, bytes, 8, 4);


            if (!BitConverter.IsLittleEndian)
                System.Array.Reverse(bytes); //reverse it so we get little endian.

            return bytes;
        }

        internal object ConvertToLogicalValue(object baseValue, DurationSchema schema)
        {
           //TODO
           return ";";
        }
    }
}
