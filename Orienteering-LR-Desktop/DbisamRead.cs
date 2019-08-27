using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.IO;
using System.Diagnostics;

namespace Orienteering_LR_Desktop
{
    // holds info about a field
    class DbisamField
    {
        public readonly string Name;
        public readonly Type DataType;
        public readonly ushort Size;
        public readonly ushort Offset;

        public DbisamField(string name, byte type, ushort size, ushort offset)
        {
            Name = name;
            DataType = GetDbisamFieldType(type);
            Size = size;
            Offset = offset;
        }

        // convert Dbisam field type bytes to a C# type
        private static Type GetDbisamFieldType(byte t)
        {
            switch (t)
            {
                case 1: // string
                    return typeof(string);
                case 2: // date
                case 5: // int16
                case 6: // int32
                case 10: // time
                case 18: // int64
                    return typeof(int);
                case 12: // uint16
                case 13: // uint32
                case 19: // uint64
                    return typeof(uint);
                case 7: // float
                case 14: // double - number of milliseconds since 1/1/0001 ?
                case 11: // timestamp
                    return typeof(double);
                case 4: // boolean
                    return typeof(bool);
                default:
                    return typeof(byte[]);
            }
        }
    }

    // some methods for reading Dbisam tables
    class DbisamRead
    {
        // takes raw table data, an offset for the current records, and DbisamField info about the desired field and returns
        // the data for that record+field in the appropriate type
        private static dynamic ReadDbisamRecordField(byte[] tableRaw, int offset, DbisamField field)
        {
            // add offset for this field to the record offset
            offset += field.Offset;

            // check if the entry is empty (first byte holds 0 = empty, 1 = non empty)
            if (tableRaw[offset++] == 0)
            {
                // return null rather than process the bytes
                return DBNull.Value;
            }
            // for strings
            else if (field.DataType == typeof(string))
            {
                // get byte data
                byte[] dataRaw = new byte[field.Size];
                Array.Copy(tableRaw, offset, dataRaw, 0, dataRaw.Length);

                // convert to string, trimming after first null char (c# strings are not null terminated)
                string data = Encoding.GetEncoding(1252).GetString(dataRaw);
                // Console.WriteLine(data.Substring(0, data.IndexOf('\0')));
                return data.Substring(0, data.IndexOf('\0'));
            }
            // (signed) ints
            else if (field.DataType == typeof(int))
            {
                // for the different data sizes
                switch (field.Size)
                {
                    case 2:
                        return BitConverter.ToInt16(tableRaw, offset);
                    case 4:
                        return BitConverter.ToInt32(tableRaw, offset);
                    case 8:
                        return BitConverter.ToInt64(tableRaw, offset);
                    default:
                        return DBNull.Value;
                }
            }
            // unsigned ints
            else if (field.DataType == typeof(uint))
            {
                switch (field.Size)
                {
                    case 2:
                        return BitConverter.ToUInt16(tableRaw, offset);
                    case 4:
                        return BitConverter.ToUInt32(tableRaw, offset);
                    case 8:
                        return BitConverter.ToUInt64(tableRaw, offset);
                    default:
                        return DBNull.Value;
                }
            }
            // float/double
            else if (field.DataType == typeof(double))
            {
                switch (field.Size)
                {
                    case 4:
                        // I don't think delphi has singles but may as well leave this for completeness
                        return BitConverter.ToSingle(tableRaw, offset);
                    case 8:
                        return BitConverter.ToDouble(tableRaw, offset);
                    default:
                        return DBNull.Value;
                }
            }
            // booleans
            else if (field.DataType == typeof(bool))
            {
                return (tableRaw[offset] != 0);
            }
            // otherwise return raw bytes
            else if (field.DataType == typeof(byte[]))
            {
                byte[] data = new byte[field.Size];
                Array.Copy(tableRaw, offset, data, 0, data.Length);
                return data;
            }
            // shouldn't get here because we've covered all the possible types
            else
            {
                return DBNull.Value;
            }
        }

        // reads a Dbisam .dat table of name tableName stored in dbPath
        public static DataTable ReadTable(string dbPath, string tableName)
        {
            try
            {
                // data structure to hold the table in
                DataTable table = new DataTable();
                table.Clear();
                table.TableName = tableName;

                // append to path if missing
                if (dbPath[dbPath.Length - 1] != '\\')
                {
                    dbPath += '\\';
                }

                // append .dat if missing from tableName
                if (!tableName.Substring(Math.Max(0, tableName.Length - 4)).Equals(".dat"))
                {
                    tableName += ".dat";
                }

                // full filename for the table file
                string filePath = dbPath + tableName;

                // read full file into memory
                byte[] tableRaw = File.ReadAllBytes(filePath);

                // table info
                int nRecords = BitConverter.ToInt32(tableRaw, 41);
                ushort recordSize = BitConverter.ToUInt16(tableRaw, 45);
                ushort nFields = BitConverter.ToUInt16(tableRaw, 47);

                // temp store field info
                DbisamField[] fields = new DbisamField[nFields];

                // offset to move past table header
                int offset = 512;

                // field headers
                for (int i = 0; i < nFields; i++)
                {
                    // initialize temp array for field name
                    byte[] fieldNameRaw = new byte[tableRaw[offset + 2]];
                    // copy string into temp byte[]
                    Array.Copy(tableRaw, offset + 3, fieldNameRaw, 0, fieldNameRaw.Length);

                    // field properties
                    byte fieldType = tableRaw[offset + 164];
                    ushort fieldSize = BitConverter.ToUInt16(tableRaw, offset + 169);
                    ushort fieldOffset = BitConverter.ToUInt16(tableRaw, offset + 172);

                    // save field info
                    fields[i] = new DbisamField(Encoding.GetEncoding(1252).GetString(fieldNameRaw), fieldType, fieldSize, fieldOffset);

                    // add column to table
                    table.Columns.Add(fields[i].Name, fields[i].DataType);

                    // Console.WriteLine(fields[i].Name + " : " + fields[i].DataType + " : " + fields[i].Size + " : " + fields[i].Offset);

                    // increment offset for each field header
                    offset += 768;
                }

                // records
                for (int i = 0; i < nRecords;)
                {
                    // if first byte is non-zero then it's deleted/error
                    if (tableRaw[offset] == 0)
                    {
                        // create a row for the record
                        DataRow row = table.NewRow();

                        // for each field
                        foreach (DbisamField field in fields)
                        {
                            row[field.Name] = ReadDbisamRecordField(tableRaw, offset, field);
                        }

                        // add row to the table
                        table.Rows.Add(row);

                        i++;
                    }

                    // increment offset for each record
                    offset += recordSize;
                }

                return table;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return null;
            }
        }
    }
}
