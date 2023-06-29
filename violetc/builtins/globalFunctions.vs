package;

public native function trace(...arguments: [*]): void;

public native function parseFloat(string: String): Number;
public native function parseInt(string: String, radix: Int? = null): Int;

public native function isNaN(value: Number): Boolean;
public native function isFinite(value: Number): Boolean;

public native function encodeURI(string: String): String;
public native function decodeURI(string: String): String;
public native function encodeURIComponent(string: String): String;
public native function decodeURIComponent(string: String): String;

public native function assert(test: Boolean, failMessage: String? = null): void;
public native function assertEqual(left: *, right: *, failMessage: String? = null): void;
public native function assertNotEqual(left: *, right: *, failMessage: String? = null): void;