import { U3DMobile } from 'csharp'

export class Log {
    public static I(...objects: any[]): void {
        this.Merge(objects, (text: string): void => {
            U3DMobile.Log.WriteInfo(text)
        })
    }

    public static Error(...objects: any[]): void {
        this.Merge(objects, (text: string): void => {
            U3DMobile.Log.WriteError(text)
        })
    }

    private static Merge(objects: any[], callback: (text: string) => void): void {
        if (objects.length == 0) {
            return
        }

        let array = new Array<string>()
        for (let thing of objects) {
            let text = this.Stringify(thing)
            array.push(text)
        }
        callback(array.join(', '))
    }

    private static Stringify(thing: any): string {
        let array = new Array<string>()
        this.Build(0, thing, array)
        return array.join('')
    }

    private static Build(level: number, thing: any, outArray: string[]): void {
        if (thing === undefined) {
            outArray.push('undefined')

        } else if (thing === null) {
            outArray.push('null')

        } else if (this.Is('Array', thing) || this.Is('Set', thing)) {
            outArray.push('[')

            let following = false
            for (let item of thing) {
                if (following) {
                    outArray.push(',')
                } else {
                    following = true
                }

                this.Build(level + 1, item, outArray)
            }

            outArray.push(']')

        } else if (this.Is('Map', thing)) {
            outArray.push('{')

            let following = false
            for (let [key, val] of thing) {
                if (following) {
                    outArray.push(',')
                } else {
                    following = true
                }

                this.Build(level + 1, key, outArray)
                outArray.push(':')
                this.Build(level + 1, val, outArray)
            }

            outArray.push('}')

        } else if (this.Is('string', thing)) {
            if (level > 0) {
                outArray.push(`"${thing}"`)
            } else {
                outArray.push(thing)
            }

        } else if (this.Is('function', thing)) {
            outArray.push(`<function ${thing.name}>`)

        } else if (this.Is('object', thing)) {
            outArray.push(JSON.stringify(thing))

        } else {
            outArray.push(String(thing))
        }
    }

    private static Is(type: string, thing: any): boolean {
        if (type == 'Map') {
            return Object.getPrototypeOf(thing).constructor == Map
        }
        if (type == 'Set') {
            return Object.getPrototypeOf(thing).constructor == Set
        }
        if (type == 'Array') {
            return Array.isArray(thing)
        }
        return typeof(thing) == type
    }
}
