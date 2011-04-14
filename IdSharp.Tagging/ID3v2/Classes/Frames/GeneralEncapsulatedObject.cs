using System;
using System.IO;
using IdSharp.Tagging.Utils;

namespace IdSharp.Tagging.ID3v2.Frames
{
    internal sealed class GeneralEncapsulatedObject : Frame, IGeneralEncapsulatedObject
    {
        private EncodingType _textEncoding;
        private string _mimeType;
        private string _fileName;
        private string _description;
        private byte[] _encapsulatedObject;

        public EncodingType TextEncoding
        {
            get { return _textEncoding; }
            set
            {
                _textEncoding = value;
                SendPropertyChanged("TextEncoding");
            }
        }

        public string MimeType
        {
            get { return _mimeType; }
            set
            {
                _mimeType = value;
                SendPropertyChanged("MimeType");
            }
        }

        public string FileName
        {
            get { return _fileName; }
            set
            {
                _fileName = value;
                SendPropertyChanged("FileName");
            }
        }

        public string Description
        {
            get { return _description; }
            set
            {
                _description = value;
                SendPropertyChanged("Description");
            }
        }

        public byte[] EncapsulatedObject
        {
            get { return ByteUtils.Clone(_encapsulatedObject); }
            set
            {
                _encapsulatedObject = ByteUtils.Clone(value);
                SendPropertyChanged("EncapsulatedObject");
            }
        }

        public override string GetFrameID(ID3v2TagVersion tagVersion)
        {
            switch (tagVersion)
            {
                case ID3v2TagVersion.ID3v24:
                case ID3v2TagVersion.ID3v23:
                    return "GEOB";
                case ID3v2TagVersion.ID3v22:
                    return "GEO";
                default:
                    throw new ArgumentException("Unknown tag version");
            }
        }

        public override void Read(TagReadingInfo tagReadingInfo, Stream stream)
        {
            _frameHeader.Read(tagReadingInfo, ref stream);

            int bytesLeft = _frameHeader.FrameSizeExcludingAdditions;
            if (bytesLeft >= 4)
            {
                TextEncoding = (EncodingType)stream.Read1(ref bytesLeft);
                MimeType = ID3v2Utils.ReadString(EncodingType.ISO88591, stream, ref bytesLeft);
                if (bytesLeft > 0)
                {
                    FileName = ID3v2Utils.ReadString(TextEncoding, stream, ref bytesLeft);
                    if (bytesLeft > 0)
                    {
                        Description = ID3v2Utils.ReadString(TextEncoding, stream, ref bytesLeft);
                        if (bytesLeft > 0)
                        {
                            EncapsulatedObject = stream.Read(bytesLeft);
                            bytesLeft = 0;
                        }
                    }
                }
            }

            // Seek to end of frame
            if (bytesLeft > 0)
            {
                stream.Seek(bytesLeft, SeekOrigin.Current);
            }
        }

        public override byte[] GetBytes(ID3v2TagVersion tagVersion)
        {
            if (_encapsulatedObject == null || _encapsulatedObject.Length == 0)
                return new byte[0];

            using (MemoryStream frameData = new MemoryStream())
            {
                frameData.WriteByte((byte)TextEncoding);
                frameData.Write(ID3v2Utils.GetStringBytes(tagVersion, EncodingType.ISO88591, MimeType, true));
                frameData.Write(ID3v2Utils.GetStringBytes(tagVersion, TextEncoding, FileName, true));
                frameData.Write(ID3v2Utils.GetStringBytes(tagVersion, TextEncoding, Description, true));
                frameData.Write(_encapsulatedObject, 0, _encapsulatedObject.Length);

                return _frameHeader.GetBytes(frameData, tagVersion, GetFrameID(tagVersion));
            }
        }
    }
}
