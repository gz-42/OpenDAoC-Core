﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace DOL.Database
{
    public abstract class WhereClause
    {
        private static string alphabet = "abcdefghijklmnopqrstuvwxyz";

        private string GetPlaceHolder(uint id)
        {
            var prefix = "@";
            uint radix = (uint)alphabet.Length;
            var result = "";
            do
            {
                var letter = alphabet[(int)(id % radix)];
                result = letter + result;
                id /= radix;
            } while (id > 0);
            return prefix + result;
        }

        public virtual string ParameterizedText
        {
            get
            {
                var clause = "WHERE ";
                uint parameterID = 0;
                foreach (var atom in IntermediateRepresentation)
                {
                    if (atom.IsParameterized)
                    {
                        clause += GetPlaceHolder(parameterID);
                        parameterID++;
                    }
                    else
                    {
                        clause += atom.Val;
                    }
                    clause += " ";
                }
                return clause.Trim();
            }
        }

        public virtual QueryParameter[] Parameters
        {
            get
            {
                var result = new List<QueryParameter>();
                uint parameterID = 0;
                foreach (var atom in IntermediateRepresentation)
                {
                    if (atom.IsParameterized)
                    {
                        result.Add(new QueryParameter(GetPlaceHolder(parameterID), atom.Val));
                        parameterID++;
                    }
                }
                return result.ToArray();
            }
        }

        internal abstract List<TextAtom> IntermediateRepresentation { get; }

        public virtual WhereClause And(WhereClause rightExpression)
            => rightExpression.Equals(Empty) ? this : new ChainingExpression(this, "AND", rightExpression);
        public virtual WhereClause Or(WhereClause rightExpression)
            => rightExpression.Equals(Empty) ? this : new ChainingExpression(this, "OR", rightExpression);
        public virtual WhereClause OrderBy(DBColumn column, bool descending = false, int limit = 0)
            => new OrderLimitExpression(this, column, descending, limit);

        public static WhereClause Empty => new EmptyWhereClause();

        public override int GetHashCode() => base.GetHashCode();
    }

    internal class TextAtom
    {
        public object Val { get; }

        public TextAtom(object val) { Val = val; }

        public virtual bool IsParameterized => false;
    }

    internal class ValueAtom : TextAtom
    {
        public ValueAtom(object val) : base(val) { }

        public override bool IsParameterized => true;
    }

    internal class EmptyWhereClause : WhereClause
    {
        internal override List<TextAtom> IntermediateRepresentation => new List<TextAtom>();

        public override WhereClause And(WhereClause rightExpression) => rightExpression;
        public override WhereClause Or(WhereClause rightExpression) => rightExpression;
        public override string ParameterizedText => string.Empty;

        public override bool Equals(object obj) => obj is EmptyWhereClause;
        public override int GetHashCode() => base.GetHashCode();
    }

    internal class FilterExpression : WhereClause
    {
        private string columnName;
        private string op;
        private object val;

        internal FilterExpression(string columnName, string op, object val)
        {
            this.columnName = columnName;
            this.op = op;
            this.val = val;
        }

        internal override List<TextAtom> IntermediateRepresentation
        {
            get
            {
                if (val is IEnumerable<object> valueCollection && valueCollection.Any())
                {
                    var result = new List<TextAtom>() { new TextAtom(columnName), new TextAtom(op), new TextAtom("(") };
                    result.Add(new ValueAtom(valueCollection.ElementAt(0)));
                    foreach(var element in valueCollection.Skip(1))
                    {
                        result.Add(new TextAtom(","));
                        result.Add(new ValueAtom(element));
                    }
                    result.Add(new TextAtom(")"));
                    return result;
                }
                return new List<TextAtom>() { new TextAtom(columnName), new TextAtom(op), new ValueAtom(val) };
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is FilterExpression filterExpression)
            {
                return filterExpression.op.Equals(op)
                    && filterExpression.columnName.Equals(columnName)
                    && filterExpression.val.Equals(val);
            }
            return false;
        }

        public override int GetHashCode() => base.GetHashCode();
    }

    internal class PlainTextExpression : WhereClause
    {
        private string columnName;
        private string op;

        internal PlainTextExpression(string columnName, string op)
        {
            this.columnName = columnName;
            this.op = op;
        }

        internal override List<TextAtom> IntermediateRepresentation
            => new List<TextAtom>() { new TextAtom(columnName), new TextAtom(op) };

        public override bool Equals(object obj)
        {
            if (obj is PlainTextExpression expression)
            {
                return expression.op.Equals(op)
                    && expression.columnName.Equals(columnName);
            }
            return false;
        }

        public override int GetHashCode() => base.GetHashCode();
    }

    internal class ChainingExpression : WhereClause
    {
        private WhereClause left;
        private WhereClause right;
        private string chainingOperator;

        internal ChainingExpression(WhereClause left, string chainingOperator, WhereClause right)
        {
            this.left = left;
            this.right = right;
            this.chainingOperator = chainingOperator;
        }

        internal override List<TextAtom> IntermediateRepresentation
        {
            get
            {
                if (right.Equals(Empty)) return left.IntermediateRepresentation;

                var result = new List<TextAtom>() { new TextAtom("(") };
                result.AddRange(left.IntermediateRepresentation);
                result.Add(new TextAtom(chainingOperator));
                result.AddRange(right.IntermediateRepresentation);
                result.Add(new TextAtom(")"));
                return result;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is ChainingExpression expr)
            {
                return expr.left.Equals(left)
                    && expr.chainingOperator.Equals(chainingOperator)
                    && expr.right.Equals(right);
            }
            return false;
        }
        public override int GetHashCode() => base.GetHashCode();
    }

    internal class OrderLimitExpression : WhereClause
    {
        private WhereClause left;
        private DBColumn column;
        private bool descending;
        private int limit;

        internal OrderLimitExpression(WhereClause left, DBColumn column, bool descending, int limit)
        {
            this.left = left;
            this.column = column;
            this.descending = descending;
            this.limit = limit;
        }

        internal override List<TextAtom> IntermediateRepresentation
        {
            get
            {
                var result = new List<TextAtom>();
                result.AddRange(left.IntermediateRepresentation);
                result.Add(new TextAtom($"ORDER BY {column.Name}"));

                if (descending)
                    result.Add(new TextAtom(" DESC"));

                if (limit > 0)
                    result.Add(new TextAtom($" LIMIT {limit}"));

                return result;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is OrderLimitExpression expr)
            {
                return expr.left.Equals(left)
                    && expr.column.Equals(column)
                    && expr.descending.Equals(descending)
                    && expr.limit.Equals(limit);
            }
            return false;
        }
        public override int GetHashCode() => base.GetHashCode();
    }

    public class DB
    {
        public static DBColumn Column(string columnName) => new DBColumn(columnName);
    }

    public class DBColumn
    {
        public string Name { get; }

        internal DBColumn(string columnName)
        {
            if (string.IsNullOrEmpty(columnName)) throw new ArgumentException("ColumnName may not be null or empty.");
            Name = columnName;
        }

        public WhereClause IsEqualTo(object val) => new FilterExpression(Name, "=", val);
        public WhereClause IsNotEqualTo(object val) => new FilterExpression(Name, "!=", val);
        public WhereClause IsGreaterThan(object val) => new FilterExpression(Name, ">", val);
        public WhereClause IsGreaterOrEqualTo(object val) => new FilterExpression(Name, ">=", val);
        public WhereClause IsLessThan(object val) => new FilterExpression(Name, "<", val);
        public WhereClause IsLessOrEqualTo(object val) => new FilterExpression(Name, "<=", val);
        public WhereClause IsLike(object val) => new FilterExpression(Name, "LIKE", val);
        public WhereClause IsNull() => new PlainTextExpression(Name, "IS NULL");
        public WhereClause IsNotNull() => new PlainTextExpression(Name, "IS NOT NULL");
        public WhereClause IsIn<T>(IEnumerable<T> values) => new FilterExpression(Name, "IN", values.Cast<object>());
    }
}
