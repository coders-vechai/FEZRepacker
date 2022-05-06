﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FEZRepacker.XNB
{
    abstract class XNBContentType
    {
        protected XNBContentConverter _converter;

        public XNBContentConverter Converter => _converter;
        public abstract TypeAssemblyQualifier Name { get; }
        public abstract Type BasicType { get; }

        public XNBContentType(XNBContentConverter converter)
        {
            _converter = converter;
        }

        public abstract object Read(BinaryReader reader);

        public abstract void Write(object data, BinaryWriter writer);
    }

    // a little helper generalized class, so I don't have to override BasicType
    abstract class XNBContentType<T> : XNBContentType
    {
        public XNBContentType(XNBContentConverter converter) : base(converter){}

        public override Type BasicType => typeof(T);
    }
}
