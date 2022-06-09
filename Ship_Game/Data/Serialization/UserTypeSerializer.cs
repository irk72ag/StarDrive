﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SDUtils;

namespace Ship_Game.Data.Serialization
{
    public abstract class UserTypeSerializer : TypeSerializer
    {
        public override string ToString() => $"UserTypeSerializer {NiceTypeName}:{TypeId}";

        // Shared Type Map for caching type serialization information
        public TypeSerializerMap TypeMap { get; }

        protected Map<string, DataField> Mapping; // field name to DataField mapping
        public DataField[] Fields;
        protected DataField PrimaryKeyName;
        protected DataField PrimaryKeyValue;


        // Method which is called when type has finished serialization
        // [StarDataDeserialized]
        // void OnDeserialized() { .. }
        public MethodInfo OnDeserialized;

        protected UserTypeSerializer(Type type, TypeSerializerMap typeMap) : base(type)
        {
            TypeMap = typeMap;
            IsUserClass = true;
            Category = SerializerCategory.UserClass;

            var a = type.GetCustomAttribute<StarDataTypeAttribute>();
            if (a == null)
                throw new InvalidDataException($"Unsupported type {type} - is the class missing [StarDataType] attribute?");
            if (a.TypeName != null)
                TypeName = a.TypeName;

            OnDeserialized = GetMethodWithAttribute<StarDataDeserialized>(type);
            // NOTE: We cannot resolve types in the constructor, it would cause a stack overflow due to nested types
        }

        static MethodInfo GetMethodWithAttribute<A>(Type type)
        {
            Type attribute = typeof(A);
            foreach (MethodInfo mi in type.GetMethods())
            {
                if (mi.GetCustomAttribute(attribute) != null)
                    return mi;
            }
            return null;
        }

        public DataField GetFieldOrNull(string fieldName)
        {
            return Mapping.TryGetValue(fieldName, out DataField f) ? f : null;
        }

        // This is somewhat slow, which is why it should be done only once,
        // and all fields should be immutable
        public void ResolveTypes()
        {
            if (Mapping != null)
                return;

            Mapping = new Map<string, DataField>();
            var index = new Array<DataField>();

            Type shouldSerialize = typeof(StarDataAttribute);
            PropertyInfo[] props = Type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            FieldInfo[]   fields = Type.GetFields(    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            var dataFields = new Array<DataField>();

            for (int i = 0; i < fields.Length; ++i)
            {
                FieldInfo f = fields[i];
                if (f.GetCustomAttribute(shouldSerialize) is StarDataAttribute a)
                {
                    var field = new DataField(TypeMap, Type, a, null, f);
                    dataFields.Add(field);
                    CheckPrimaryKeys(a, field);
                }
            }
            
            for (int i = 0; i < props.Length; ++i)
            {
                PropertyInfo p = props[i];
                if (p.GetCustomAttribute(shouldSerialize) is StarDataAttribute a)
                {
                    var field = new DataField(TypeMap, Type, a, p, null);
                    dataFields.Add(field);
                    CheckPrimaryKeys(a, field);
                }
            }

            if (dataFields.IsEmpty)
            {
                Log.Warning($"[StarDataType] {NiceTypeName} has no [StarData] fields, consider not serializing it!");
                return;
            }

            foreach (DataField field in dataFields)
            {
                field.FieldIdx = index.Count;
                Mapping.Add(field.Name, field);
                index.Add(field);
            }

            Fields = index.ToArr();
        }

        void CheckPrimaryKeys(StarDataAttribute a, DataField field)
        {
            if (a.IsPrimaryKeyName)
            {
                if (PrimaryKeyName != null)
                    throw new InvalidDataException($"[StarDataType] {NiceTypeName} cannot have more than 1 [StarDataKeyName] attributes! Original {PrimaryKeyValue}, New {field}");
                PrimaryKeyName = field;
            }
            else if (a.IsPrimaryKeyValue)
            {
                if (PrimaryKeyValue != null)
                    throw new InvalidDataException($"[StarDataType] {NiceTypeName} cannot have more than 1 [StarDataKeyValue] attributes! Original {PrimaryKeyValue}, New {field}");
                PrimaryKeyValue = field;
            }
        }
    }
}
