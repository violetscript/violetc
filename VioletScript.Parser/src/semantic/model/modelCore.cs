namespace VioletScript.Parser.Semantic.Model;

using System.Collections.Generic;
using VioletScript.Parser.Operator;

public sealed class ModelCore {
    public Factory Factory;
    /// <summary>
    /// Built-in object.
    /// </summary>
    public Symbol GlobalPackage = null;
    /// <summary>
    /// The <c>*</c> type.
    /// </summary>
    public Symbol AnyType = null;
    /// <summary>
    /// The <c>undefined</c> type.
    /// </summary>
    public Symbol UndefinedType = null;
    /// <summary>
    /// The <c>null</c> type.
    /// </summary>
    public Symbol NullType = null;
    /// <summary>
    /// Built-in object.
    /// </summary>
    /// <remarks>
    /// The definition for the Object type
    /// has no <c>DontInit</c> decorator, allowing all subclasses
    /// to be constructed via an object initialiser by default.
    /// </remarks>
    public Symbol ObjectType = null;
    /// <summary>
    /// Built-in method. It is overriden by enums.
    /// </summary>
    public Symbol ObjectValueOfMethod = null;
    /// <summary>
    /// Built-in method. It is overriden by enums.
    /// </summary>
    public Symbol ObjectToStringMethod = null;
    /// <summary>
    /// Built-in object.
    /// </summary>
    public Symbol StringType = null;
    /// <summary>
    /// Built-in object.
    /// </summary>
    public Symbol BooleanType = null;
    /// <summary>
    /// Built-in object.
    /// </summary>
    public Symbol NumberType = null;
    /// <summary>
    /// Built-in object.
    /// </summary>
    public Symbol DecimalType = null;
    /// <summary>
    /// Built-in object.
    /// </summary>
    public Symbol ByteType = null;
    /// <summary>
    /// Built-in object.
    /// </summary>
    public Symbol ShortType = null;
    /// <summary>
    /// Built-in object.
    /// </summary>
    public Symbol IntType = null;
    /// <summary>
    /// Built-in object.
    /// </summary>
    public Symbol LongType = null;
    /// <summary>
    /// Built-in object.
    /// </summary>
    public Symbol BigIntType = null;
    /// <summary>
    /// Built-in object.
    /// </summary>
    public Symbol IteratorType = null;
    /// <summary>
    /// Built-in object.
    /// </summary>
    public Symbol FunctionType = null;
    /// <summary>
    /// Built-in object.
    /// </summary>
    public Symbol ArrayType = null;
    /// <summary>
    /// Built-in object.
    /// </summary>
    public Symbol MapType = null;
    /// <summary>
    /// Built-in object.
    /// </summary>
    public Symbol SetType = null;
    /// <summary>
    /// Built-in object.
    /// </summary>
    public Symbol PromiseType = null;
    /// <summary>
    /// Built-in object.
    /// </summary>
    public Symbol GeneratorType = null;
    /// <summary>
    /// Built-in object.
    /// </summary>
    public Symbol ClassType = null;
    /// <summary>
    /// Built-in object.
    /// </summary>
    public Symbol ByteArrayType = null;
    /// <summary>
    /// Built-in object.
    /// </summary>
    public Symbol RegExpType = null;
    /// <summary>
    /// Built-in object.
    /// </summary>
    public Symbol BindingType = null;
    /// <summary>
    /// Built-in object.
    /// </summary>
    public Symbol IMarkupContainerType = null;
    /// <summary>
    /// Built-in object.
    /// </summary>
    public Symbol IDisposableType = null;

    private Dictionary<int, List<Symbol>> m_InternedRecordTypesByFieldCount = new Dictionary<int, List<Symbol>>{};
    private Dictionary<int, List<Symbol>> m_InternedTupleTypesByItemCount = new Dictionary<int, List<Symbol>>{};
    private Dictionary<int, List<Symbol>> m_InternedUnionTypesByMCount = new Dictionary<int, List<Symbol>>{};
    private Dictionary<int, List<Symbol>> m_InternedFuncTypesByReqParamCount = new Dictionary<int, List<Symbol>>{};
    private Dictionary<Symbol, List<Symbol>> m_InternedInstantiatedTypesByOrigin = new Dictionary<Symbol, List<Symbol>>{};
    private Dictionary<Symbol, Dictionary<Symbol[], List<Symbol>>> m_InternedInstantiatedVarSlotsByOrigin = new Dictionary<Symbol, Dictionary<Symbol[], List<Symbol>>>{};
    private Dictionary<Symbol, Dictionary<Symbol[], List<Symbol>>> m_InternedInstantiatedVirtualSlotsByOrigin = new Dictionary<Symbol, Dictionary<Symbol[], List<Symbol>>>{};
    private Dictionary<Symbol, Dictionary<Symbol[], List<Symbol>>> m_InternedInstantiatedMethodSlotsByOrigin = new Dictionary<Symbol, Dictionary<Symbol[], List<Symbol>>>{};
    private Dictionary<Symbol, List<Symbol>> m_InternedInstantiationOfGenericMethodsByOrigin = new Dictionary<Symbol, List<Symbol>>{};

    public ModelCore() {
        Factory = new Factory(this);
        InitializeBuiltins();
    }

    private void InitializeBuiltins() {
        GlobalPackage = Factory.Package("");
        Factory.AnyType();
        Factory.UndefinedType();
        Factory.NullType();

        this.ObjectType = DefineGlobalBuiltinClass("Object", false);

        this.StringType = DefineGlobalBuiltinClass("String", true, true);
        this.StringType.DontInit = true;

        this.BooleanType = DefineGlobalBuiltinClass("Boolean", true, true);
        this.BooleanType.DontInit = true;

        this.NumberType = DefineGlobalBuiltinClass("Number", true, true);
        this.NumberType.DontInit = true;

        this.DecimalType = DefineGlobalBuiltinClass("Decimal", true, true);
        this.DecimalType.DontInit = true;

        this.ByteType = DefineGlobalBuiltinClass("Byte", true, true);
        this.ByteType.DontInit = true;

        this.ShortType = DefineGlobalBuiltinClass("Short", true, true);
        this.ShortType.DontInit = true;

        this.IntType = DefineGlobalBuiltinClass("Int", true, true);
        this.IntType.DontInit = true;

        this.LongType = DefineGlobalBuiltinClass("Long", true, true);
        this.LongType.DontInit = true;

        this.BigIntType = DefineGlobalBuiltinClass("BigInt", true, true);
        this.BigIntType.DontInit = true;

        this.IteratorType = DefineGlobalBuiltinInterface("Iterator");
        this.IteratorType.TypeParameters = new Symbol[]{Factory.TypeParameter("T")};

        this.BindingType = DefineGlobalBuiltinClass("Binding", true);

        this.FunctionType = DefineGlobalBuiltinClass("Function", false);
        this.FunctionType.DontInit = true;

        this.ArrayType = DefineGlobalBuiltinClass("Array", true);
        this.ArrayType.DontInit = true;
        this.ArrayType.TypeParameters = new Symbol[]{Factory.TypeParameter("T")};

        this.MapType = DefineGlobalBuiltinClass("Map", true);
        this.MapType.TypeParameters = new Symbol[]{
            Factory.TypeParameter("K"),
            Factory.TypeParameter("V")
        };
        this.MapType.DontInit = true;

        this.SetType = DefineGlobalBuiltinClass("Set", true);
        this.SetType.TypeParameters = new Symbol[]{Factory.TypeParameter("T")};
        this.SetType.DontInit = true;

        this.PromiseType = DefineGlobalBuiltinClass("Promise", true);
        this.PromiseType.TypeParameters = new Symbol[]{Factory.TypeParameter("T")};
        this.PromiseType.DontInit = true;

        this.GeneratorType = DefineGlobalBuiltinClass("Generator", true);
        this.GeneratorType.TypeParameters = new Symbol[]{Factory.TypeParameter("T")};
        this.GeneratorType.DontInit = true;

        this.ClassType = DefineGlobalBuiltinClass("Class", false);
        this.ClassType.DontInit = true;

        this.ByteArrayType = DefineGlobalBuiltinClass("ByteArray", true);
        this.ByteArrayType.DontInit = true;

        this.RegExpType = DefineGlobalBuiltinClass("RegExp", true);
        this.RegExpType.DontInit = true;

        this.IMarkupContainerType = DefineGlobalBuiltinInterface("IMarkupContainer");
        this.IMarkupContainerType.TypeParameters = new Symbol[]{Factory.TypeParameter("T")};

        this.IDisposableType = DefineGlobalBuiltinInterface("IDisposable");

        // global
        this.GlobalPackage.Properties.Set("global", GlobalPackage);

        // undefined
        var undefinedConstant = Factory.VariableSlot("undefined", true, UndefinedType);
        undefinedConstant.InitValue = Factory.UndefinedConstantValue(UndefinedType);
        undefinedConstant.Visibility = Visibility.Public;
        undefinedConstant.ParentDefinition = this.GlobalPackage;
        this.GlobalPackage.Properties.Set("undefined", undefinedConstant);

        // NaN
        var nanConstant = Factory.VariableSlot("NaN", true, NumberType);
        nanConstant.InitValue = Factory.NumberConstantValue(double.NaN, NumberType);
        nanConstant.Visibility = Visibility.Public;
        nanConstant.ParentDefinition = this.GlobalPackage;
        this.GlobalPackage.Properties.Set("NaN", nanConstant);

        // Infinity
        var infConstant = Factory.VariableSlot("Infinity", true, NumberType);
        infConstant.InitValue = Factory.NumberConstantValue(double.PositiveInfinity, NumberType);
        infConstant.Visibility = Visibility.Public;
        infConstant.ParentDefinition = this.GlobalPackage;
        this.GlobalPackage.Properties.Set("Infinity", infConstant);

        // Number.MIN_VALUE
        var numberMinConstant = Factory.VariableSlot("MIN_VALUE", true, NumberType);
        numberMinConstant.InitValue = Factory.NumberConstantValue(double.MinValue, NumberType);
        numberMinConstant.Visibility = Visibility.Public;
        numberMinConstant.ParentDefinition = this.NumberType;
        NumberType.Properties.Set("MIN_VALUE", numberMinConstant);

        // Number.MAX_VALUE
        var numberMaxConstant = Factory.VariableSlot("MAX_VALUE", true, NumberType);
        numberMaxConstant.InitValue = Factory.NumberConstantValue(double.MaxValue, NumberType);
        numberMaxConstant.Visibility = Visibility.Public;
        numberMaxConstant.ParentDefinition = this.NumberType;
        NumberType.Properties.Set("MAX_VALUE", numberMaxConstant);

        // Long.MIN_VALUE
        var longMinConstant = Factory.VariableSlot("MIN_VALUE", true, LongType);
        longMinConstant.InitValue = Factory.LongConstantValue(long.MinValue, LongType);
        longMinConstant.Visibility = Visibility.Public;
        longMinConstant.ParentDefinition = this.LongType;
        LongType.Properties.Set("MIN_VALUE", longMinConstant);

        // Long.MAX_VALUE
        var longMaxConstant = Factory.VariableSlot("MAX_VALUE", true, LongType);
        longMaxConstant.InitValue = Factory.LongConstantValue(long.MaxValue, LongType);
        longMaxConstant.Visibility = Visibility.Public;
        longMaxConstant.ParentDefinition = this.LongType;
        LongType.Properties.Set("MAX_VALUE", longMaxConstant);

        // object.valueOf()
        var objValueOfMethodSt = Factory.FunctionType(null, null, null, AnyType);
        this.ObjectValueOfMethod = Factory.MethodSlot("valueOf", objValueOfMethodSt, MethodSlotFlags.Native);
        this.ObjectValueOfMethod.Visibility = Visibility.Public;
        this.ObjectValueOfMethod.ParentDefinition = this.ObjectType;
        ObjectType.Delegate.Properties["valueOf"] = ObjectValueOfMethod;

        // object.toString()
        var objToStringMethodSt = Factory.FunctionType(null, null, null, StringType);
        this.ObjectToStringMethod = Factory.MethodSlot("toString", objToStringMethodSt, MethodSlotFlags.Native);
        this.ObjectToStringMethod.Visibility = Visibility.Public;
        this.ObjectToStringMethod.ParentDefinition = this.ObjectType;
        ObjectType.Delegate.Properties["toString"] = ObjectToStringMethod;
    }

    private Symbol DefineGlobalBuiltinClass(string name, bool isFinal = true, bool isValue = false) {
        isFinal = isFinal || isValue;
        var r = Factory.ClassType(name, isFinal, isValue);
        r.Visibility = Visibility.Public;
        GlobalPackage.Properties[name] = r;
        return r;
    }

    private Symbol DefineGlobalBuiltinInterface(string name) {
        var r = Factory.InterfaceType(name);
        r.Visibility = Visibility.Public;
        GlobalPackage.Properties.Set(name, r);
        return r;
    }

    public Symbol InternRecordType(NameAndTypePair[] fields) {
        var n = fields.Count();
        if (!m_InternedRecordTypesByFieldCount.ContainsKey(n)) {
            m_InternedRecordTypesByFieldCount[n] = new List<Symbol>{};
        }
        var list = m_InternedRecordTypesByFieldCount[n];
        foreach (var type2 in list) {
            var type2_Fields = type2.RecordTypeFields;
            var eq = true;
            for (int i = 0; i < n; ++i) {
                if (fields[i] != type2_Fields[i]) {
                    eq = false;
                    break;
                }
            }
            if (eq) return type2;
        }
        var r = new RecordType(fields);
        r.ModelCore = this;
        r.Delegate = Factory.Delegate(r);
        foreach (var (f_Name, f_Type) in fields) {
            var f_Slot = Factory.VariableSlot(f_Name, true, f_Type);
            r.Delegate.Properties[f_Name] = f_Slot;
        }
        list.Add(r);
        return r;
    }

    public Symbol InternTupleType(Symbol[] items) {
        var n = items.Count();
        if (!m_InternedTupleTypesByItemCount.ContainsKey(n)) {
            m_InternedTupleTypesByItemCount[n] = new List<Symbol>{};
        }
        var list = m_InternedTupleTypesByItemCount[n];
        foreach (var type2 in list) {
            var type2_Items = type2.TupleElementTypes;
            var eq = true;
            for (int i = 0; i < n; ++i) {
                if (items[i] != type2_Items[i]) {
                    eq = false;
                    break;
                }
            }
            if (eq) return type2;
        }
        var r = new TupleType(items);
        r.ModelCore = this;
        list.Add(r);
        return r;
    }

    public Symbol InternUnionType(Symbol[] members) {
        if (members.Count() == 1) {
            return members[0];
        }
        var members_Spread = new List<Symbol>{};
        foreach (var m_0 in members) {
            if (m_0 is AnyType) {
                return AnyType;
            }
            if (m_0 is UnionType) {
                foreach (var nestedM in m_0.UnionMemberTypes) {
                    if (!members_Spread.Contains(nestedM)) {
                        members_Spread.Add(nestedM);
                    }
                }
            } else if (!members_Spread.Contains(m_0)) {
                members_Spread.Add(m_0);
            }
        }
        members = members_Spread.ToArray();
        var n = members.Count();
        if (n == 1) {
            return members[0];
        }
        if (!m_InternedUnionTypesByMCount.ContainsKey(n)) {
            m_InternedUnionTypesByMCount[n] = new List<Symbol>{};
        }
        var list = m_InternedUnionTypesByMCount[n];
        foreach (var type2 in list) {
            var type2_M = type2.UnionMemberTypes;
            var eq = true;
            for (int i = 0; i < n; ++i) {
                if (members[i] != type2_M[i]) {
                    eq = false;
                    break;
                }
            }
            if (eq) return type2;
        }
        var r = new UnionType(members);
        r.ModelCore = this;
        list.Add(r);
        return r;
    }

    public Symbol InternFunctionType(NameAndTypePair[] @params, NameAndTypePair[] optParams, NameAndTypePair? restParam, Symbol returnType) {
        var n = @params != null ? @params.Count() : 0;
        if (!m_InternedFuncTypesByReqParamCount.ContainsKey(n)) {
            m_InternedFuncTypesByReqParamCount[n] = new List<Symbol>{};
        }
        var list = m_InternedFuncTypesByReqParamCount[n];
        foreach (var type2 in list) {
            var eq = returnType == type2.FunctionReturnType
                &&   restParam == type2.FunctionRestParameter;
            if (!eq) {
                continue;
            }

            // compare required parameters
            if (@params != null) {
                var type2_ReqParams = type2.FunctionRequiredParameters;
                for (int i = 0; i < n; ++i) {
                    if (@params[i] != type2_ReqParams[i]) {
                        eq = false;
                        break;
                    }
                }
                if (!eq) continue;
            }

            // compare optional parameters
            if ((optParams != null && !type2.FunctionHasOptParameters)
            ||  (optParams == null && type2.FunctionHasOptParameters)) {
                continue;
            }
            if (optParams != null) {
                var optParamsN = optParams.Count();
                if (optParamsN != type2.FunctionCountOfOptParameters) {
                    continue;
                }
                var type2_OptParams = type2.FunctionOptParameters;
                for (int i = 0; i < optParamsN; ++i) {
                    if (optParams[i] != type2_OptParams[i]) {
                        eq = false;
                        break;
                    }
                }
            }

            if (eq) return type2;
        }
        var r = new FunctionType(@params, optParams, restParam, returnType);
        r.ModelCore = this;
        list.Add(r);
        return r;
    }

    public Symbol InternInstantiatedType(Symbol origin, Symbol[] argumentsList) {
        if (!m_InternedInstantiatedTypesByOrigin.ContainsKey(origin)) {
            m_InternedInstantiatedTypesByOrigin[origin] = new List<Symbol>{};
        }
        var list = m_InternedInstantiatedTypesByOrigin[origin];

        var originParams = origin.TypeParameters;
        var n = originParams.Count();
        var isOrigin = true;

        for (int j = 0; j < n; ++j) {
            if (originParams[j] != argumentsList[j]) {
                isOrigin = false;
                break;
            }
        }
        if (isOrigin) return origin;

        foreach (var type2 in list) {
            var type2_Types = type2.ArgumentTypes;
            var eq = true;
            for (int i = 0; i < n; ++i) {
                if (argumentsList[i] != type2_Types[i]) {
                    eq = false;
                    break;
                }
            }
            if (eq) return type2;
        }
        var r = new InstantiatedType(origin, argumentsList);
        r.ModelCore = this;
        list.Add(r);
        return r;
    }

    public Symbol InternInstantiatedVariableSlot(Symbol origin, Symbol[] relTypeParams, Symbol[] argumentsList) {
        if (!m_InternedInstantiatedVarSlotsByOrigin.ContainsKey(origin)) {
            m_InternedInstantiatedVarSlotsByOrigin[origin] = new Dictionary<Symbol[], List<Symbol>>{};
        }
        var list1 = m_InternedInstantiatedVarSlotsByOrigin[origin];
        if (!list1.ContainsKey(relTypeParams)) {
            list1[relTypeParams] = new List<Symbol>{};
        }
        var list2 = list1[relTypeParams];
        var n = argumentsList.Count();
        foreach (var slot2 in list2) {
            var eq = true;
            var args2 = slot2.ArgumentsToRelatedParameterizedType;
            for (int i = 0; i < n; ++i) {
                if (argumentsList[i] != args2[i]) {
                    eq = false;
                    break;
                }
            }
            if (eq) return slot2;
        }
        var r = new InstantiatedVariableSlot(relTypeParams, argumentsList, origin, origin.StaticType.ReplaceTypes(relTypeParams, argumentsList));
        r.ModelCore = this;
        list2.Add(r);
        return r;
    }

    public Symbol InternInstantiatedVirtualSlot(Symbol origin, Symbol[] relTypeParams, Symbol[] argumentsList) {
        if (!m_InternedInstantiatedVirtualSlotsByOrigin.ContainsKey(origin)) {
            m_InternedInstantiatedVirtualSlotsByOrigin[origin] = new Dictionary<Symbol[], List<Symbol>>{};
        }
        var list1 = m_InternedInstantiatedVirtualSlotsByOrigin[origin];
        if (!list1.ContainsKey(relTypeParams)) {
            list1[relTypeParams] = new List<Symbol>{};
        }
        var list2 = list1[relTypeParams];
        var n = argumentsList.Count();
        foreach (var slot2 in list2) {
            var eq = true;
            var args2 = slot2.ArgumentsToRelatedParameterizedType;
            for (int i = 0; i < n; ++i) {
                if (argumentsList[i] != args2[i]) {
                    eq = false;
                    break;
                }
            }
            if (eq) return slot2;
        }
        var r = new InstantiatedVirtualSlot(relTypeParams, argumentsList, origin, origin.StaticType.ReplaceTypes(relTypeParams, argumentsList));
        r.ModelCore = this;
        list2.Add(r);
        return r;
    }

    public Symbol InternInstantiatedMethodSlot(Symbol origin, Symbol[] relTypeParams, Symbol[] argumentsList) {
        if (!m_InternedInstantiatedMethodSlotsByOrigin.ContainsKey(origin)) {
            m_InternedInstantiatedMethodSlotsByOrigin[origin] = new Dictionary<Symbol[], List<Symbol>>{};
        }
        var list1 = m_InternedInstantiatedMethodSlotsByOrigin[origin];
        if (!list1.ContainsKey(relTypeParams)) {
            list1[relTypeParams] = new List<Symbol>{};
        }
        var list2 = list1[relTypeParams];
        var n = argumentsList.Count();
        foreach (var slot2 in list2) {
            var eq = true;
            var args2 = slot2.ArgumentsToRelatedParameterizedType;
            for (int i = 0; i < n; ++i) {
                if (argumentsList[i] != args2[i]) {
                    eq = false;
                    break;
                }
            }
            if (eq) return slot2;
        }
        var r = new InstantiatedMethodSlot(relTypeParams, argumentsList, origin, origin.StaticType.ReplaceTypes(relTypeParams, argumentsList));
        r.ModelCore = this;
        list2.Add(r);
        return r;
    }

    public Symbol InternInstantiationOfGenericMethodSlot(Symbol origin, Symbol[] argumentsList) {
        if (!m_InternedInstantiationOfGenericMethodsByOrigin.ContainsKey(origin)) {
            m_InternedInstantiationOfGenericMethodsByOrigin[origin] = new List<Symbol>{};
        }
        var list = m_InternedInstantiationOfGenericMethodsByOrigin[origin];
        var n = argumentsList.Count();
        foreach (var slot2 in list) {
            var eq = true;
            var args2 = slot2.ArgumentTypes;
            for (int i = 0; i < n; ++i) {
                if (argumentsList[i] != args2[i]) {
                    eq = false;
                    break;
                }
            }
            if (eq) return slot2;
        }
        var r = new InstantiationOfGenericMethodSlot(origin, argumentsList, origin.StaticType.ReplaceTypes(origin.TypeParameters, argumentsList));
        r.ModelCore = this;
        list.Add(r);
        return r;
    }

    public bool IsNumericType(Symbol type) {
        return type == NumberType || type == DecimalType
            || type == ByteType || type == ShortType
            || type == IntType || type == LongType
            || type == BigIntType;
    }

    public bool IsIntegerType(Symbol type) {
        return type == ByteType || type == ShortType
            || type == IntType || type == LongType
            || type == BigIntType;
    }
}