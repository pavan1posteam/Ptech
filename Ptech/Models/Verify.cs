using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ptech.Models
{
    class Verify
    {
        private readonly DataRow _row;

        private readonly int _storeId;
        public Verify() { }
        public Verify(DataRow row, int storeId)
        {
            _storeId = storeId;
            _row = row;
        }
        public string GetStringByIndex(int index)
        {
            if (index >= _row.ItemArray.Length)
                throw new Exception($"Missing column at index {index} in store {_storeId}");

            return _row[index]?.ToString().Trim() ?? "";
        }
        public int GetIntByIndex(int index)
        {
            if (index >= _row.ItemArray.Length)
                throw new Exception($"Missing column at index {index} in store {_storeId}");

            var value = _row[index];

            try
            {
                if (value == null || Convert.IsDBNull(value))
                    return 0;

                if (decimal.TryParse(value.ToString(), out decimal result))
                {
                    return (int)Math.Floor(result);
                }
                string val = GetStringByIndex(index);
                if (!int.TryParse(val, out int result1))
                    return 0;
                else
                    return result1;
            }
            catch (Exception)
            {
                throw new Exception($"Invalid integer value at index {index} in store {_storeId}. Value: '{value}'");
            }
        }
        public decimal GetDecimalByIndex(int index)
        {
            if (index >= _row.ItemArray.Length)
                throw new Exception($"Missing column at index {index} in store {_storeId}");

            var value = _row[index];

            try
            {
                if (value == null || Convert.IsDBNull(value))
                    return 0;

                if (decimal.TryParse(value.ToString(), out decimal result))
                {
                    return result;
                }
                string val = GetStringByIndex(index);
                if (!decimal.TryParse(val, out decimal result1))
                    return 0;
                else
                    return result1;
            }
            catch (Exception)
            {
                throw new Exception($"Invalid decimal value at index {index} in store {_storeId}. Value: '{value}'");
            }
        }
        public int getpack(string prodName)
        {
            prodName = prodName.ToUpper();
            var regexMatch = Regex.Match(prodName, @"(?<Result>\d+)PK");
            var prodPack = regexMatch.Groups["Result"].Value;
            if (prodPack.Length > 0)
            {
                int outVal = 0;
                int.TryParse(prodPack.Replace("$", ""), out outVal);
                return outVal;
            }
            return 1;
        }
        public string getVolume(string prodName)
        {
            prodName = prodName.ToUpper();
            var regexMatch = Regex.Match(prodName, @"(?<Result>\d+)ML| (?<Result>\d+)LTR| (?<Result>\d+)OZ | (?<Result>\d+)L|(?<Result>\d+)OZ");
            var prodPack = regexMatch.Groups["Result"].Value;
            if (prodPack.Length > 0)
            {
                return regexMatch.ToString();
            }
            return "";
        }
    }
}
