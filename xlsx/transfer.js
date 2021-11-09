const InputXlsxDir = './defines'
const OutputTsFile = '../tsproj/manuscript/data/xlsx.ts'

const FSystem  = require('fs')
const Path     = require('path')
const Process  = require('process')
const XlsxRead = require('read-excel-file/node')

//------------------------------------------------------------------------------
//type recognition:

/**
 * @param  {?string} string
 * @return {boolean}
 */
function isNullOrSpace(string) {
    return !string || /^\s*$/.test(string)
}

/**
 * @param  {string} string
 * @return {boolean}
 */
function isWord(string) {
    return /^\w+$/.test(string)
}

/**
 * @param  {string} string
 * @return {boolean}
 */
function isDigit(string) {
    return /^\d+$/.test(string)
}

/**
 * @param  {string} chr
 * @return {number}
 */
function digit(chr) {
    return chr.charCodeAt(0) - '0'.charCodeAt(0)
}

/**
 * @param  {number} num
 * @return {string}
 */
function alphabeta(num) {
    return String.fromCharCode('A'.charCodeAt(0) + num)
}

/**
 * @typedef Index
 * @type    {Object}
 * @prop    {number} value
 */

/**
 * @param  {Index}  index
 * @param  {string} buffer
 * @return {?string}
 */
function untilChar(index, buffer) {
    while (index.value < buffer.length) {
        let chr = buffer[index.value]
        if (isNullOrSpace(chr)) {
            index.value++
        } else {
            return chr
        }
    }
    return null
}

const Type_Integer  = 'int'
const Type_Float    = 'float'
const Type_String   = 'string'
const Type_IntArray = '[int]'
const Type_FltArray = '[float]'
const Type_StrArray = '[string]'

const TypeRecognizers = new Map(Object.entries({
    [Type_Integer ]: /^\s*int\s*$/,
    [Type_Float   ]: /^\s*float\s*$/,
    [Type_String  ]: /^\s*string\s*$/,
    [Type_IntArray]: /^\s*\[\s*int\s*\]\s*$/,
    [Type_FltArray]: /^\s*\[\s*float\s*\]\s*$/,
    [Type_StrArray]: /^\s*\[\s*string\s*\]\s*$/,
}))

/**
 * @param  {Index}  index
 * @param  {string} buffer
 * @return {?number}
 */
function read_integer(index, buffer) {
    let value = read_float(index, buffer)
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
function read_float(index, buffer) {
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

/**
 * @param  {string} terminator
 * @param  {Index}  index
 * @param  {string} buffer
 * @return {?string}
 */
function read_stringEndsWith(terminator, index, buffer) {
    let array = []

    while (index.value < buffer.length) {
        let chr = buffer[index.value++]

        if (chr == '\\') {
            if (index.value < buffer.length) {
                chr = buffer[index.value++]
                switch (chr) {
                    case 'n': array.push('\n'); break
                    case ',': array.push(',' ); break

                    //unrecognized escape characters remain as they are.
                    default: array.push(`\\${chr}`)
                }
            } else {
                array.push('\\')
            }
        } else if (chr == terminator) {
            break
        } else {
            array.push(chr)
        }
    }

    return array.length != 0 ? array.join('') : null
}

/**
 * @param  {Index}  index
 * @param  {string} buffer
 * @return {?string}
 */
function read_string(index, buffer) {
    return read_stringEndsWith('\0', index, buffer)
}

/**
 * @param  {Index}  index
 * @param  {string} buffer
 * @return {?string}
 */
function read_stringEndsWithComma(index, buffer) {
    return read_stringEndsWith(',', index, buffer)
}

/**
 * @param  {Index}  index
 * @param  {string} buffer
 * @param  {(index:Index, buffer:string) => ?Object} itemReader
 * @return {?Object[]}
 */
function read_array(index, buffer, itemReader) {
    let head = untilChar(index, buffer)
    if (head != '[') {
        return null
    }

    //it's maybe an empty array.
    let next = untilChar(index, buffer)
    if (next == ']') {
        index.value++
        return []
    }

    while (true) {
        let item = itemReader(index, buffer)
        if (item == null) {
            return null
        }

        let draw = untilChar(index, buffer)
        if (draw == ',') {
            index.value++
            continue
        } else if (draw == ']') {
            index.value++
            return array
        } else {
            return null
        }
    }
}

/**
 * @param  {Index}  index
 * @param  {string} buffer
 * @return {?number[]}
 */
function read_intArray(index, buffer) {
    return read_array(index, buffer, read_integer)
}

/**
 * @param  {Index}  index
 * @param  {string} buffer
 * @return {?number[]}
 */
function read_fltArray(index, buffer) {
    return read_array(index, buffer, read_float)
}

/**
 * @param  {Index}  index
 * @param  {string} buffer
 * @return {?string[]}
 */
function read_strArray(index, buffer) {
    return read_array(index, buffer, read_stringEndsWithComma)
}

/**
 * @param  {string} cell
 * @param  {Object} alter
 * @param  {(index:Index, buffer:string) => ?Object} reader
 * @return {?Object}
 */
function read_cell(cell, alter, reader) {
    if (isNullOrSpace(cell)) {
        //if the cell is space, return the default value.
        return alter
    }

    let index = { value: 0 }
    let value = reader(index, cell)
    if (value == null) {
        //abnormal value.
        return null
    }

    let tail = untilChar(index, cell)
    if (tail != null) {
        //there are characters remaining, regarding it as an error.
        return null
    }

    return value
}

/** @type {Map<string, (tag:string, cell:string) => Object>} */
const TypeReaders = new Map()

TypeReaders.set(Type_Integer, (tag, cell) => {
    let alter = 0
    let value = read_cell(cell, alter, read_integer)
    if (value != null) {
        return value
    } else {
        console.log(`WARNING: ${tag} "${cell}" is not a integer`)
        return alter
    }
})

TypeReaders.set(Type_Float, (tag, cell) => {
    let alter = 0
    let value = read_cell(cell, alter, read_float)
    if (value != null) {
        return value
    } else {
        console.log(`WARNING: ${tag} "${cell}" is not a float`)
        return alter
    }
})

TypeReaders.set(Type_String, (tag, cell) => {
    let alter = ''
    let value = read_cell(cell, alter, read_string)
    if (value != null) {
        return value
    } else {
        console.log(`WARNING: ${tag} "${cell}" is not a string`)
        return alter
    }
})

TypeReaders.set(Type_IntArray, (tag, cell) => {
    let alter = []
    let value = read_cell(cell, alter, read_intArray)
    if (value != null) {
        return value
    } else {
        console.log(`WARNING: ${tag} "${cell}" is not a integer array`)
        return alter
    }
})

TypeReaders.set(Type_FltArray, (tag, cell) => {
    let alter = []
    let value = read_cell(cell, alter, read_fltArray)
    if (value != null) {
        return value
    } else {
        console.log(`WARNING: ${tag} "${cell}" is not a float array`)
        return alter
    }
})

TypeReaders.set(Type_StrArray, (tag, cell) => {
    let alter = []
    let value = read_cell(cell, alter, read_strArray)
    if (value != null) {
        return value
    } else {
        console.log(`WARNING: ${tag} "${cell}" is not a string array`)
        return alter
    }
})

//------------------------------------------------------------------------------
//typescript generation:

/**
 * @typedef StructSheet
 * @type    {Object}
 * @prop    {string}   file
 * @prop    {string}   name
 * @prop    {string[]} members
 * @prop    {string[]} types
 * @prop    {string[]} comments
 * @prop    {Object[]} dataRows
 */

/** @param {StructSheet[]} structSheets */
function writeTsFile(structSheets) {
    let lines = []
    append_namespace(lines, structSheets)

    let codes = lines.join('')
    FSystem.writeFileSync(OutputTsFile, codes)
}

/**
 * @param {string[]}      outLines
 * @param {StructSheet[]} structSheets
 */
function append_namespace(outLines, structSheets) {
    let head = 'export namespace xlsx {\r\n'
    let tail = '}\r\n'

    //head.
    outLines.push(head)

    //body.
    for (let sheet of structSheets) {
        append_class(outLines, sheet)
        append_data (outLines, sheet)

        outLines.push('\r\n')
    }

    //tail.
    outLines.push(tail)
}

/**
 * @param {string[]}    outLines
 * @param {StructSheet} sheet
 */
function append_class(outLines, sheet) {
    let head = '    export class {NAME}Row {\r\n'
    let body = '        readonly {MEMB}: {TYPE} //{CMMT}\r\n'
    let tail = '    }\r\n'

    //head.
    let headLine = head.replace(/{NAME}/g, (flag) => {
        return {
            "{NAME}": sheet.name
        }[flag]
    })
    outLines.push(headLine)

    //body.
    for (let index = 0; index < sheet.types.length; ++index) {
        let iden = sheet.types[index]
        let type = {
            [Type_Integer ]: 'number',
            [Type_Float   ]: 'number',
            [Type_String  ]: 'string',
            [Type_IntArray]: 'Array<number>',
            [Type_FltArray]: 'Array<number>',
            [Type_StrArray]: 'Array<String>',
        }[iden]

        let memb = sheet.members [index]
        let cmmt = sheet.comments[index]

        let bodyLine = body.replace(/{MEMB}|{TYPE}|{CMMT}/g, (flag) => {
            return {
                '{MEMB}': memb,
                '{TYPE}': type,
                '{CMMT}': cmmt,
            }[flag]
        })
        outLines.push(bodyLine)
    }

    //tail.
    outLines.push(tail)
}

/**
 * @param {string[]}    outLines
 * @param {StructSheet} sheet
 */
function append_data(outLines, sheet) {
    let head = '    export const {NAME}Table: {NAME}Row[] = [\r\n'
    let ibgn = '        {'
    let ied0 = '},\r\n'
    let ied1 = '}\r\n'
    let tail = '    ]\r\n'

    //head.
    let headLine = head.replace(/{NAME}/g, (flag) => {
        return {
            "{NAME}": sheet.name
        }[flag]
    })
    outLines.push(headLine)

    //body.
    for (let row = 0; row < sheet.dataRows.length; ++row) {
        outLines.push(ibgn)

        for (let col = 0; col < sheet.members.length; ++col) {
            let memb = sheet.members[col]
            outLines.push(memb)
            outLines.push(':')

            let item = sheet.dataRows[row][col]
            append_item(outLines, item)
            if (col < sheet.members.length - 1) {
                outLines.push(',')
            }
        }

        if (row < sheet.dataRows.length - 1) {
            outLines.push(ied0)
        } else {
            outLines.push(ied1)
        }
    }

    //tail.
    outLines.push(tail)
}

/**
 * @param {string[]} outLines
 * @param {any}      item
 */
function append_item(outLines, item) {
    if (Array.isArray(item)) {
        outLines.push('[')
        for (let index = 0; index < item.length; ++index) {
            append_item(outLines, item)
            if (index < item.length - 1) {
                outLines.push(',')
            }
        }
        outLines.push(']')

    } else if (typeof(item) == 'string') {
        outLines.push("'")
        for (let chr of item) {
            switch (chr) {
                case '\"': outLines.push('\\"'); break
                case '\'': outLines.push("\\'"); break
                case '\r': outLines.push('\\r'); break
                case '\n': outLines.push('\\n'); break
                case '\t': outLines.push('\\t'); break
                default  : outLines.push(chr)
            }
        }
        outLines.push("'")

    } else if (typeof(item) == 'number') {
        outLines.push(`${item}`)
    }
}

//------------------------------------------------------------------------------
//sheets traverse:

/**
 * @typedef SheetPath
 * @type    {Object}
 * @prop    {string} file
 * @prop    {string} name
 */

/**
 * @typedef RawSheet
 * @type    {Object}
 * @prop    {string}   file
 * @prop    {string}   name
 * @prop    {string[]} rows
 */

function main() {
    enterCurrentDir()

    let files = collectFiles(InputXlsxDir)
    if (files.length == 0) {
        console.log('ERROR: not found any xlsx files')
        return
    }

    collectSheets(files, (sheets) => {
        if (sheets.length == 0) {
            console.log('ERROR: not found any xlsx sheets')
            return
        }

        let makingNameMap = new Map()
        let makingSheets = []
        forEachSheet(sheets, (rawSheet, isLast) => {
            let structSheet = transferSheet(rawSheet)

            //ignore exception sheets. do not terminate the program.
            if (!structSheet) {
                return
            }

            if (makingNameMap.has(structSheet.name)) {
                console.log(`WARNING: duplicate sheet name "${structSheet.name}"`)
                console.log(`    first: ${makingNameMap.get(structSheet.name)}`)
                console.log(`    again: ${structSheet.file}`)
                return
            }
            makingNameMap.set(structSheet.name, structSheet.file)
            makingSheets.push(structSheet)

            if (isLast) {
                if (makingSheets.length == 0) {
                    console.log('ERROR: not found available xlsx sheets')
                } else {
                    writeTsFile(makingSheets)
                }
            }
        })
    })
}

function enterCurrentDir() {
    Process.chdir(__dirname)
}

/**
 * @param  {string} dir
 * @return {string[]}
 */
function collectFiles(dir) {
    let files = []

    let subitems = FSystem.readdirSync(dir)
    for (let item of subitems) {
        if (item.startsWith('.') || item.startsWith('~')) {
            //NOTE: ignore these files.
            //ms office and wps may create temporary files with the "xlsx" extension.
            continue
        }

        let path = Path.join(dir, item)
        let stat = FSystem.statSync(path)

        if (stat.isDirectory()) {
            let rest = collectFiles(path)
            files = [ ...files, ...rest ]

        } else if (path.endsWith('.xlsx')) {
            files.push(path)
        }
    }

    return files
}

/**
 * @param {string[]}                      files
 * @param {(sheets: SheetPath[]) => void} callback
 */
function collectSheets(files, callback) {
    let sheets = []
    collectSheetsInner(0, files, sheets, () => {
        callback(sheets)
    })
}

/**
 * @param {number}      startIndex
 * @param {string[]}    files
 * @param {SheetPath[]} outSheets
 * @param {() => void}  callback
 */
function collectSheetsInner(startIndex, files, outSheets, callback) {
    if (startIndex == files.length) {
        callback()
        return
    }

    let currentFile = files[startIndex]
    XlsxRead(currentFile, { getSheets: true }).then((sheetNames) => {
        do {
            if (!sheetNames || sheetNames.length == 0) {
                break
            }

            for (let name of sheetNames) {
                let sheet = {
                    file: currentFile,
                    name: name.name,
                }
                outSheets.push(sheet)
            }
        } while (false)

        collectSheetsInner(startIndex + 1, files, outSheets, callback)
    })
}

/**
 * @param {SheetPath[]}                                 sheets
 * @param {(rawSheet:RawSheet, isLast:boolean) => void} handler
 */
function forEachSheet(sheets, handler) {
    forEachSheetInner(0, sheets, handler)
}

/**
 * @param {number}                                      startIndex
 * @param {SheetPath[]}                                 sheets
 * @param {(rawSheet:RawSheet, isLast:boolean) => void} handler
 */
function forEachSheetInner(startIndex, sheets, handler) {
    if (startIndex == sheets.length) {
        return
    }

    let currentSheet = sheets[startIndex]
    XlsxRead(currentSheet.file, { sheet: currentSheet.name }).then((rows) => {
        //NOTE: the members of 'rows' may not be strings.
        let copyRows = []
        for (let objectRow of rows) {
            let stringRow = []
            for (let object of objectRow) {
                if (object == null) {
                    stringRow.push('')
                } else if (typeof(object) == 'string') {
                    stringRow.push(object)
                } else {
                    stringRow.push(String(object))
                }
            }
            copyRows.push(stringRow)
        }

        let rawSheet = {
            file: currentSheet.file,
            name: currentSheet.name,
            rows: copyRows
        }
        handler(rawSheet, startIndex == sheets.length - 1)

        forEachSheetInner(startIndex + 1, sheets, handler)
    })
}

/**
 * @param  {RawSheet} rawSheet
 * @return {?StructSheet}
 */
function transferSheet(rawSheet) {
    let name = checkoutTableName(rawSheet)
    if (!name) {
        return null
    }

    let needCol = []
    let members = checkoutMembers(rawSheet, needCol)
    if (!members) {
        return null
    }

    let types    = checkoutTypes   (rawSheet, needCol       ); if (!types   ) { return null }
    let comments = checkoutComments(rawSheet, needCol       ); if (!comments) { return null }
    let dataRows = checkoutDataRows(rawSheet, needCol, types); if (!dataRows) { return null }

    return {
        file    : rawSheet.file,
        name    : name,
        members : members,
        types   : types,
        comments: comments,
        dataRows: dataRows,
    }
}

/**
 * @param  {RawSheet} rawSheet
 * @return {?string}
 */
function checkoutTableName(rawSheet) {
    //here use sheet name as table name.
    if (isWord(rawSheet.name)) {
        return rawSheet.name
    } else {
        console.log(`WARNING: ${rawSheet.file} "${rawSheet.name}" can't as a class name`)
        return null
    }
}

/**
 * @param  {RawSheet}  rawSheet
 * @param  {boolean[]} outNeedCol
 * @return {?string[]}
 */
function checkoutMembers(rawSheet, outNeedCol) {
    //here use the first line as the member variable names.
    let row = 1

    let tag = `${rawSheet.file}/${rawSheet.name}`
    if (rawSheet.rows.length < row) {
        console.log(`WARNING: ${tag} requires line ${row} is member names`)
        return null
    }

    let memberRow = rawSheet.rows[row - 1]
    if (!memberRow || memberRow.length == 0) {
        console.log(`WARNING: ${tag} line ${row} is emptry`)
        return null
    }

    let members = []
    for (let cell of memberRow) {
        if (isWord(cell)) {
            outNeedCol.push(true)
            members.push(cell)
        } else {
            //allow illegal names. it is common to ignore certain columns.
            outNeedCol.push(false)
        }
    }
    if (outNeedCol.length == 0) {
        console.log(`WARNING: ${tag} line ${row} don't contain legal member names`)
        return null
    }

    return members
}

/**
 * @param {RawSheet}  rawSheet
 * @param {boolean[]} needCol
 * @param {?string[]}
 */
function checkoutTypes(rawSheet, needCol) {
    //here use the second line as the member variable types.
    let row = 2

    let tag = `${rawSheet.file}/${rawSheet.name}`
    if (rawSheet.rows.length < row) {
        console.log(`WARNING: ${tag} requires line ${row} is types`)
        return null
    }

    let typeRow = rawSheet.rows[row - 1]
    if (!typeRow || typeRow.length == 0) {
        console.log(`WARNING: ${tag} line ${row} is emptry`)
        return null
    }

    let types = []
    for (let col = 0; col < needCol.length; ++col) {
        if (!needCol[col]) {
            continue
        }

        let cell = typeRow[col]
        let iden = null
        for (let [id, pattern] of TypeRecognizers) {
            if (pattern.test(cell)) {
                iden = id
                break
            }
        }
        if (iden == null) {
            let loc = `(${row}, ${alphabeta(col)})`
            console.log(`WARNING: ${tag} ${loc} is unexpected type`)
            return null
        }

        types.push(iden)
    }

    return types
}

/**
 * @param {RawSheet}  rawSheet
 * @param {boolean[]} needCol
 * @param {?string[]}
 */
function checkoutComments(rawSheet, needCol) {
    //here use the third line as the comments.
    let row = 3

    let tag = `${rawSheet.file}/${rawSheet.name}`
    if (rawSheet.rows.length < row) {
        console.log(`WARNING: ${tag} requires line ${row} is comments`)
        return null
    }

    let commentRow = rawSheet.rows[row - 1]
    if (!commentRow || commentRow.length == 0) {
        console.log(`WARNING: ${tag} line ${row} is emptry`)
        return null
    }

    let comments = []
    for (let col = 0; col < needCol.length; ++col) {
        if (!needCol[col]) {
            continue
        }

        //NOTE: comments maybe contain newlines.
        let cell = commentRow[col]
        cell = cell.replace(/(\n|\r|\r\n)+/, ' ')
        comments.push(cell)
    }

    return comments
}

/**
 * @param {RawSheet}  rawSheet
 * @param {boolean[]} needCol
 * @param {string[]}  types
 * @param {?Object[]}
 */
function checkoutDataRows(rawSheet, needCol, types) {
    //here data rows start from the fourth line.
    let from = 4

    let dataRows = []
    for (let row = from; row <= rawSheet.rows.length; ++row) {
        let srcRow = rawSheet.rows[row - 1]
        let dstRow = []

        let validCol = 0
        for (let col = 0; col < srcRow.length; ++col) {
            if (!needCol[col]) {
                continue
            }

            let typeId = types[validCol++]
            let reader = TypeReaders.get(typeId)
            if (!reader) {
                //this branch will not be run under normal circumstances.
                return null
            }

            let coord = `${rawSheet.file}/${rawSheet.name} (${row}, ${alphabeta(col)})`
            let cell  = srcRow[col]
            let value = reader(coord, cell)
            if (value == null) {
                return null
            }

            dstRow.push(value)
        }

        dataRows.push(dstRow)
    }

    return dataRows
}

main()
