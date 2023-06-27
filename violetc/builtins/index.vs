/*
include './globalConstants';
include './globalFunctions';
include './object';
include './collections';
include './primitiveTypes';
include './bytearray';
include './regexp';
include './function';
include './promise';
include './errors';
include './math';
include './reflect';
include './observable';
include './intl';
include './temporal';
*/
class C.<T> {
    function C() {}
    function f.<W>(arg: W): void {}
}
new C.<Int>().f.<Int>('')