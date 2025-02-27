﻿using FEZRepacker.Converter.XNB.Formats;

namespace FEZRepacker.Converter.XNB.Types.System
{
    internal class ListContentType<T> : XnbContentType<List<T>> where T : notnull
    {
        private XnbAssemblyQualifier _name;
        private bool _skipElementType;
        public ListContentType(XnbFormatConverter converter, bool skipElementType = false) : base(converter)
        {
            // creating type assembly qualifier name
            _name = typeof(ArrayContentType<T>).FullName ?? "";
            _name.Namespace = "Microsoft.Xna.Framework.Content";
            _name.Name = "ListReader";

            var genericQualifier = GetXnbTypeFor(typeof(T));
            if (genericQualifier.HasValue) _name.Templates[0] = genericQualifier.Value;

            // similarly to arrays, elements can have type identifier prefix
            // but unlike arrays, this is much less common
            // again, barely any idea what's the rule here.
            _skipElementType = skipElementType;
        }

        public override XnbAssemblyQualifier Name => _name;

        public override object Read(BinaryReader reader)
        {
            List<T> data = new List<T>();
            int dataCount = reader.ReadInt32();
            for (int i = 0; i < dataCount; i++)
            {
                T? value = Converter.ReadType<T>(reader, _skipElementType);
                if (value != null) data.Add(value);
            }
            return data;
        }

        public override void Write(object data, BinaryWriter writer)
        {
            List<T> list = (List<T>)data;

            writer.Write(list.Count);
            foreach (T value in list)
            {
                Converter.WriteType<T>(value, writer);
            }
        }

        public override bool IsEmpty(object data)
        {
            return ((List<T>)data).Count == 0;
        }
    }
}
