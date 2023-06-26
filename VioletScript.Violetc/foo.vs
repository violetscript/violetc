function f(): Promise.<String> {
    const x = await f2();
    return '';
}
native function f2(): Promise.<Number>;
var x: Number?;
x?.toString();