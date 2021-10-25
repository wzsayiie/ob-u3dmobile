import { U3DMobile } from 'csharp'

export class Log
{
    static I(format: any, ...params: any[]): void
    {
        let formatString = this.Stringify(format)
        let paramStrings = this.StringifyArray(params)

        U3DMobile.Log.I(formatString, ...paramStrings)
    }

    static Error(format: any, ...params: any[]): void
    {
        let formatString = this.Stringify(format)
        let paramStrings = this.StringifyArray(params)

        U3DMobile.Log.Error(formatString, ...paramStrings)
    }

    private static Stringify(src: any): string
    {
        let array = new Array<string>()
        this.Append(src, array)

        return array.join('')
    }

    private static StringifyArray(srcArray: any[]): string[]
    {
        let dstArray = new Array<string>()
        for (let src of srcArray)
        {
            let dst = this.Stringify(src)
            dstArray.push(dst)
        }
        return dstArray
    }

    private static Append(anything: any, out: string[]): void
    {
        if (anything === null)
        {
            out.push('null')
        }
        else if (this.Is('Array', anything) || this.Is('Set', anything))
        {
            let first = true

            out.push('[')
            for (let item of anything)
            {
                if (first)
                {
                    first = false
                }
                else
                {
                    out.push(',')
                }

                this.Append(item, out)
            }
            out.push(']')
        }
        else if (this.Is('Map', anything))
        {
            let first = true

            out.push('{')
            for (let [key, val] of anything)
            {
                if (first)
                {
                    first = false
                }
                else
                {
                    out.push(',')
                }

                this.Append(key, out)
                out.push(':')
                this.Append(val, out)
            }
            out.push('}')
        }
        else if (this.Is('function', anything))
        {
            out.push('{function}')
        }
        else if (this.Is('object', anything))
        {
            //NOTE: do not traverse an object,
            //the number of its child nodes may be very large.
            out.push(`{${anything}}`)
        }
        else
        {
            out.push(String(anything))
        }
    }

    private static Is(type: string, anything: any): boolean
    {
        if (type == 'Array')
        {
            return anything && Array.isArray(anything)
        }
        if (type == 'Map')
        {
            return anything && Object.getPrototypeOf(anything).constructor == Map
        }
        if (type == 'Set')
        {
            return anything && Object.getPrototypeOf(anything).constructor == Set
        }

        return typeof anything == type
    }
}
