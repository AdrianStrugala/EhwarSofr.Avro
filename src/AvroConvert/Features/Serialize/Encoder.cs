﻿#region license
/**
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

/** Modifications copyright(C) 2020 Adrian Strugała **/
#endregion

using System;
using System.IO;
using SolTechnology.Avro.AvroObjectServices.FileHeader;
using SolTechnology.Avro.AvroObjectServices.FileHeader.Codec;
using SolTechnology.Avro.AvroObjectServices.Schema.Abstract;
using SolTechnology.Avro.AvroObjectServices.Write;
using SolTechnology.Avro.Infrastructure.Exceptions;

namespace SolTechnology.Avro.Features.Serialize
{
    internal class Encoder : IDisposable
    {
        internal delegate void WriteItem(object value, IWriter encoder);

        private readonly TypeSchema _schema;
        private readonly AbstractCodec _codec;
        private readonly Stream _stream;
        private MemoryStream _tempBuffer;
        private readonly Writer _writer;
        private IWriter _tempWriter;
        private readonly WriteItem _writeItem;
        private bool _isOpen;
        private bool _headerWritten;
        private int _blockCount;
        private readonly int _syncInterval;
        private readonly Header _header;


        internal Encoder(TypeSchema schema, Stream outStream, CodecType codecType)
        {
            _codec = AbstractCodec.CreateCodec(codecType);
            _stream = outStream;
            _header = new Header();
            _schema = schema;
            _syncInterval = DataFileConstants.DefaultSyncInterval;

            _blockCount = 0;
            _writer = new Writer(_stream);
            _tempBuffer = new MemoryStream();
            _tempWriter = new Writer(_tempBuffer);

            GenerateSyncData();
            _header.AddMetadata(DataFileConstants.CodecMetadataKey, _codec.Name);
            _header.AddMetadata(DataFileConstants.SchemaMetadataKey, _schema.ToString());

            _writeItem = Resolver.ResolveWriter(schema);

            _isOpen = true;
        }

        internal void Append(object datum)
        {
            AssertOpen();
            EnsureHeader();

            _writeItem(datum, _tempWriter);

            _blockCount++;
            WriteIfBlockFull();
        }

        private void EnsureHeader()
        {
            if (!_headerWritten)
            {
                WriteHeader();
                _headerWritten = true;
            }
        }

        internal long Sync()
        {
            AssertOpen();
            WriteBlock();
            return _stream.Position;
        }

        private void WriteHeader()
        {
            _writer.WriteHeader(_header);
        }

        private void AssertOpen()
        {
            if (!_isOpen) throw new AvroRuntimeException("Cannot complete operation: avro file/stream not open");
        }

        private void WriteIfBlockFull()
        {
            if (_tempBuffer.Position >= _syncInterval)
                WriteBlock();
        }

        private void WriteBlock()
        {
            if (_blockCount > 0)
            {
                byte[] dataToWrite = _tempBuffer.ToArray();

                _writer.WriteDataBlock(_codec.Compress(dataToWrite), _header.SyncData, _blockCount);

                // reset block buffer
                _blockCount = 0;
                _tempBuffer = new MemoryStream();
                _tempWriter = new Writer(_tempBuffer);
            }
        }

        private void GenerateSyncData()
        {
            _header.SyncData = new byte[16];

            Random random = new Random();
            random.NextBytes(_header.SyncData);
        }

        public void Dispose()
        {
            EnsureHeader();
            Sync();
            _stream.Flush();
            _stream.Dispose();
            _isOpen = false;
        }
    }
}
