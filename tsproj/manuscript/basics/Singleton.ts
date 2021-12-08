export function Singleton<T>() {
    class Super {
        private static s_instance: any

        public static GetInstance(): T {
            if (!this.s_instance) {
                this.s_instance = new this()
            }
            return this.s_instance
        }

        public static get instance(): T {
            return this.GetInstance()
        }
    }
    return Super
}
