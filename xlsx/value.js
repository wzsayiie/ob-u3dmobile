const TypeEnum = {
    Int      : 'int'            ,
    Float    : 'float'          ,
    String   : 'string'         ,

    IntArray : '[int]'          ,
    FltArray : '[float]'        ,
    StrArray : '[string]'       ,

    IntIntMap: '{int:int}'      ,
    IntFltMap: '{int:float}'    ,
    IntStrMap: '{int:string}'   ,
    StrIntMap: '{string:int}'   ,
    StrFltMap: '{string:float}' ,
    StrStrMap: '{string:string}',
}

/**
 * @param  {string} type
 * @param  {string} buffer
 * @return {?*}
 */
function readValue(type, buffer) {
    let reader = ValueReader[type]

    let index = { value: 0 }
    let value = reader(index, buffer)
    if (value == null) {
        //abnormal value.
        return null
    }

    let chr = untilChar(index, buffer)
    if (chr != null) {
        //there are characters remaining, regarding it as an error.
        return null
    }

    return value
}

const ValueReader = {
    [TypeEnum.Int      ]: read_Int      ,
    [TypeEnum.Float    ]: read_Float    ,
    [TypeEnum.String   ]: read_SoleStr  ,

    [TypeEnum.IntArray ]: read_IntArray ,
    [TypeEnum.FltArray ]: read_FltArray ,
    [TypeEnum.StrArray ]: read_StrArray ,

    [TypeEnum.IntIntMap]: read_IntIntMap,
    [TypeEnum.IntFltMap]: read_IntFltMap,
    [TypeEnum.IntStrMap]: read_IntStrMap,
    [TypeEnum.StrIntMap]: read_StrIntMap,
    [TypeEnum.StrFltMap]: read_StrFltMap,
    [TypeEnum.StrStrMap]: read_StrStrMap,
}

/**
 * @typedef  {Object} Index
 * @property {number} value
 */

/**
 * @param  {Index}  index
 * @param  {string} buffer
 * @return {?number}
 */
function read_Int(index, buffer) {
    let value = read_Float(index, buffer)
    if (value != null) {
        return Math.floor(value)
    } else {
        return null
    }
}

/**
 * @param  {Index}  index
 * @param  {string} buffer
 * @return {?number}
 */
function read_Float(index, buffer) {
    let chr = untilChar(index, buffer)
    if (chr == null) {
        return null
    }

    /** @type {?number } */ let sign = null
    /** @type {?number } */ let ints = null
    /** @type {?boolean} */ let spot = null
    /** @type {?number } */ let decs = null

    let base = 1

    while (index.value < buffer.length) {
        chr = buffer[index.value]

        if (sign == null) {
            if (isDigit(chr)) {
                index.value++
                sign = 1
                ints = digit(chr)
            } else if (chr == '+') {
                index.value++
                sign = 1
            } else if (chr == '-') {
                index.value++
                sign = -1
            } else {
                return null
            }

        } else if (ints == null) {
            if (isDigit(chr)) {
                index.value++
                ints = digit(chr)
            } else {
                return null
            }

        } else if (spot == null) {
            if (isDigit(chr)) {
                index.value++
                ints = ints * 10 + digit(chr)
            } else if (chr == '.') {
                index.value++
                spot = true
                continue
            } else {
                //this number only contains integer part.
                break
            }

        } else if (decs == null) {
            if (isDigit(chr)) {
                index.value++
                base = base * 0.1
                decs = base * digit(chr)
            } else {
                return null
            }

        } else {
            if (isDigit(chr)) {
                index.value++
                base = base * 0.1
                decs = base * digit(chr) + decs
            } else {
                //this number contains integer and decimal parts.
                break
            }
        }
    }

    if (ints != null && decs != null) {
        return sign * (ints + decs)
    } else if (ints != null) {
        return sign * ints
    } else {
        return null
    }
}

function read_SoleStr(idx, buf) { return read_String(idx, buf, []) }
function read_PartStr(idx, buf) { return read_String(idx, buf, [',', ']', ':', '}']) }

/**
 * @param  {Index}    index
 * @param  {string}   buffer
 * @param  {string[]} terminators
 * @return {?string}
 */
function read_String(index, buffer, terminators) {
    let ends = new Set(terminators)

    //ignore spaces at the head.
    untilChar(index, buffer)

    let array = []
    while (index.value < buffer.length) {
        let chr = buffer[index.value]

        if (chr == '\\') {
            index.value++

            if (index.value == buffer.length) {
                //keep the "\" at the end.
                array.push('\\')
                break
            }

            chr = buffer[index.value++]
            switch (chr) {
                case '\\': array.push('\\'); break
                case 'n' : array.push('\n'); break
                case 'r' : array.push('\r'); break
                case 't' : array.push('\t'); break
                case '[' : array.push('[' ); break
                case ']' : array.push(']' ); break
                case '{' : array.push('{' ); break
                case '}' : array.push('}' ); break
                case ',' : array.push(',' ); break
                case ':' : array.push(':' ); break
                case ' ' : array.push(' ' ); break

                //unrecognized escape characters remain as they are.
                default: array.push(`\\${chr}`)
            }

        } else if (ends.has(chr)) {
            break

        } else {
            index.value++
            array.push(chr)
        }
    }

    return array.length > 0 ? array.join('') : null
}

function read_IntArray(idx, buf) { return read_Array(idx, buf, read_Int    ) }
function read_FltArray(idx, buf) { return read_Array(idx, buf, read_Float  ) }
function read_StrArray(idx, buf) { return read_Array(idx, buf, read_PartStr) }

/**
 * @param  {Index}                                 index
 * @param  {string}                                buffer
 * @param  {(index: number, buffer: string) => ?*} itemReader
 * @return {?[]}
 */
function read_Array(index, buffer, itemReader) {
    let chr = untilChar(index, buffer)
    if (chr == '[') {
        index.value++
    } else {
        return null
    }

    //it's maybe an empty array.
    chr = untilChar(index, buffer)
    if (chr == ']') {
        index.value++
        return []
    }

    let array = []
    while (true) {
        let item = itemReader(index, buffer)
        if (item != null) {
            array.push(item)
        } else {
            return null
        }

        let gap = untilChar(index, buffer)
        if (gap == ',') {
            index.value++
            continue
        } else if (gap == ']') {
            index.value++
            return array
        } else {
            return null
        }
    }
}

function read_IntIntMap(idx, buf) { return read_Map(idx, buf, read_Int    , read_Int    ) }
function read_IntFltMap(idx, buf) { return read_Map(idx, buf, read_Int    , read_Float  ) }
function read_IntStrMap(idx, buf) { return read_Map(idx, buf, read_Int    , read_PartStr) }
function read_StrIntMap(idx, buf) { return read_Map(idx, buf, read_PartStr, read_Int    ) }
function read_StrFltMap(idx, buf) { return read_Map(idx, buf, read_PartStr, read_Float  ) }
function read_StrStrMap(idx, buf) { return read_Map(idx, buf, read_PartStr, read_PartStr) }

/**
 * @param  {Index}                                 index
 * @param  {string}                                buffer
 * @param  {(index: number, buffer: string) => ?*} keyReader
 * @param  {(index: number, buffer: string) => ?*} valueReader
 * @return {?{}}
 */
function read_Map(index, buffer, keyReader, valueReader) {
    let chr = untilChar(index, buffer)
    if (chr == '{') {
        index.value++
    } else {
        return null
    }

    //it's maybe an empty map.
    chr = untilChar(index, buffer)
    if (chr == '}') {
        index.value++
        return {}
    }

    let map = {}
    while (true) {
        let key = keyReader(index, buffer)
        if (key == null) {
            return null
        }

        let middle = untilChar(index, buffer)
        if (middle == ':') {
            index.value++
        } else {
            return null
        }

        let value = valueReader(index, buffer)
        if (value == null) {
            return null
        }

        map[key] = value

        let gap = untilChar(index, buffer)
        if (gap == ',') {
            index.value++
            continue
        } else if (gap == '}') {
            index.value++
            return map
        } else {
            return null
        }
    }
}

/**
 * @param  {Index}  index
 * @param  {string} buffer
 * @return {?string}
 */
function untilChar(index, buffer) {
    while (index.value < buffer.length) {
        let chr = buffer[index.value]
        if (isSpace(chr)) {
            index.value++
        } else {
            return chr
        }
    }
    return null
}

/**
 * @param  {string} text
 * @return {boolean}
 */
function isSpace(text) {
    return /^\s*$/.test(text)
}

/**
 * @param  {string} text
 * @return {boolean}
 */
function isDigit(text) {
    return /^\d+$/.test(text)
}

/**
 * @param  {string} chr
 * @return {number}
 */
function digit(chr) {
    return chr.charCodeAt(0) - '0'.charCodeAt(0)
}

module.exports = {
    TypeEnum : TypeEnum ,
    readValue: readValue,
}
