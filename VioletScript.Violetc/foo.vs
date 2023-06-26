package q.b.f {
    [Flags]
    public enum E {
        X
        Y
        Z
        public function f(): void {
            trace(this.include('xx'));
        }
    }
}
import q.b.f.*
const fooBarQux: E = ['x']
fooBarQux.f();
0 + 10