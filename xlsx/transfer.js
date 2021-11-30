const InputXlsxDir = './definitions'
const OutputTsFile = '../tsproj/manuscript/data/xlsx.ts'

const fs   = require('fs'     )
const proc = require('process')
const xval = require('./value')
const xlsx = require('./xlsx' )

function main() {
    enterCurrentDir()

    xlsx.readXlsx(InputXlsxDir, (sheets) => {
        if (sheets.length == 0) {
            return error(null, null, 'not found any xlsx sheets')
        }

        let generator = new TsGenerator()

        for (let sheet of sheets) {
            let norm = readNorm(sheet)
            if (!norm) {
                return
            }

            let data = readData(sheet, norm)
            if (!data) {
                return
            }

            generator.write(norm, data)
        }

        generator.save()
    })
}

function enterCurrentDir() {
    proc.chdir(__dirname)
}

const TypeForm = {
    Int      : /^\s*int\s*$/   ,
    Float    : /^\s*float\s*$/ ,
    String   : /^\s*string\s*$/,

    IntArray : /^\s*\[\s*int\s*\]\s*$/   ,
    FltArray : /^\s*\[\s*float\s*\]\s*$/ ,
    StrArray : /^\s*\[\s*string\s*\]\s*$/,

    IntIntMap: /^\s*\{\s*int\s*:\s*int\s*\}\s*$/      ,
    IntFltMap: /^\s*\{\s*int\s*:\s*float\s*\}\s*$/    ,
    IntStrMap: /^\s*\{\s*int\s*:\s*string\s*\}\s*$/   ,
    StrIntMap: /^\s*\{\s*string\s*:\s*int\s*\}\s*$/   ,
    StrFltMap: /^\s*\{\s*string\s*:\s*float\s*\}\s*$/ ,
    StrStrMap: /^\s*\{\s*string\s*:\s*string\s*\}\s*$/,
}

/**
 * @typedef  {Object}    TableNorm
 * @property {string}    name
 * @property {boolean[]} ignores
 * @property {string[]}  members
 * @property {string[]}  types
 * @property {string[]}  comments
 */

/**
 * @param  {xlsx.XlsxSheet} sheet
 * @return {?TableNorm}
 */
function readNorm(sheet) {
    let norm = {}

    //read the table name.
    if (!sheet.sheetName.match(/^([A-Z]|[a-z]|_)\w*$/)) {
        return error(sheet, null, 'illegal table name')
    }

    norm.name = sheet.sheetName

    //read the members.
    let memberRI = 0
    if (memberRI >= sheet.dataCells.length) {
        return error(sheet, { ri: memberRI, ci: ci }, 'member name row is missing')
    }

    let rawMembers = sheet.dataCells[memberRI]
    let selIgnores = []
    let selMembers = []

    for (let ci = 0; ci < rawMembers.length; ++ci) {
        let member = rawMembers[ci]

        //NOTE: if there is not member name, ignore this column.
        if (!member) {
            selIgnores.push(true)
            selMembers.push('')
            continue
        }

        if (!member.match(/^([A-Z]|[a-z]|_)\w*$/)) {
            return error(sheet, { ri: memberRI, ci: ci }, 'illegal member name')
        }

        selIgnores.push(false)
        selMembers.push(member)
    }

    norm.ignores = selIgnores
    norm.members = selMembers

    //read the types.
    let typeRI = 1
    if (typeRI >= sheet.dataCells.length) {
        return error(sheet, { ri: typeRI, ci: 0 }, 'type row is missing')
    }

    let rawTypes = sheet.dataCells[typeRI]
    let selTypes = []

    for (let ci = 0; ci < rawTypes.length; ++ci) {
        if (selIgnores[ci]) {
            selTypes.push('')
            continue
        }

        let type = rawTypes[ci]
        if (!type) {
            return error(sheet, { ri: typeRI, ci: ci }, 'empty type string')
        }

        if /**/ (type.match(TypeForm.Int      )) { selTypes.push(xval.TypeEnum.Int      ) }
        else if (type.match(TypeForm.Float    )) { selTypes.push(xval.TypeEnum.Float    ) }
        else if (type.match(TypeForm.String   )) { selTypes.push(xval.TypeEnum.String   ) }
        else if (type.match(TypeForm.IntArray )) { selTypes.push(xval.TypeEnum.IntArray ) }
        else if (type.match(TypeForm.FltArray )) { selTypes.push(xval.TypeEnum.FltArray ) }
        else if (type.match(TypeForm.StrArray )) { selTypes.push(xval.TypeEnum.StrArray ) }
        else if (type.match(TypeForm.IntIntMap)) { selTypes.push(xval.TypeEnum.IntIntMap) }
        else if (type.match(TypeForm.IntFltMap)) { selTypes.push(xval.TypeEnum.IntFltMap) }
        else if (type.match(TypeForm.IntStrMap)) { selTypes.push(xval.TypeEnum.IntStrMap) }
        else if (type.match(TypeForm.StrIntMap)) { selTypes.push(xval.TypeEnum.StrIntMap) }
        else if (type.match(TypeForm.StrFltMap)) { selTypes.push(xval.TypeEnum.StrFltMap) }
        else if (type.match(TypeForm.StrStrMap)) { selTypes.push(xval.TypeEnum.StrStrMap) }
        else {
            return error(sheet, { ri: typeRI, ci: ci }, 'unexpected type')
        }
    }

    norm.types = selTypes

    //read the comments.
    let commentRI = 2
    if (commentRI >= sheet.dataCells.length) {
        return error(sheet, { ri: commentRI, ci: 0 }, 'comment row is missing')
    }

    let rawComments = sheet.dataCells[commentRI]
    let selComments = []

    for (let ci = 0; ci < rawComments.length; ++ci) {
        if (selIgnores[ci]) {
            selComments.push('')
            continue
        }

        let comment = rawComments[ci]
        if (comment) {
            //NOTE: comments maybe contain endlines.
            selComments.push(comment.replace(/\s+/g, ' '))
        } else {
            selComments.push('')
        }
    }

    norm.comments = selComments

    return norm
}

/**
 * @typedef  {Object}   TableData
 * @property {Object[]} rows
 */

/**
 * @param  {xlsx.XlsxSheet} sheet
 * @param  {TableNorm}      norm
 * @return {?TableData}
 */
function readData(sheet, norm) {
    let rows = []

    for (let ri = 3; ri < sheet.dataCells.length; ++ri) {
        let row = {}

        for (let ci = 0; ci < norm.types.length; ++ci) {
            if (norm.ignores[ci]) {
                continue
            }

            let cell = sheet.dataCells[ri][ci]
            let name = norm.members[ci]

            //if the cell is empty, set the default value.
            if (!cell) {
                if /**/ (type == xval.TypeEnum.Int      ) { row[name] = 0  }
                else if (type == xval.TypeEnum.Float    ) { row[name] = 0  }
                else if (type == xval.TypeEnum.String   ) { row[name] = '' }
                else if (type == xval.TypeEnum.IntArray ) { row[name] = [] }
                else if (type == xval.TypeEnum.FltArray ) { row[name] = [] }
                else if (type == xval.TypeEnum.StrArray ) { row[name] = [] }
                else if (type == xval.TypeEnum.IntIntMap) { row[name] = {} }
                else if (type == xval.TypeEnum.IntFltMap) { row[name] = {} }
                else if (type == xval.TypeEnum.IntStrMap) { row[name] = {} }
                else if (type == xval.TypeEnum.StrIntMap) { row[name] = {} }
                else if (type == xval.TypeEnum.StrFltMap) { row[name] = {} }
                else if (type == xval.TypeEnum.StrStrMap) { row[name] = {} }

                continue
            }

            let type  = norm.types[ci]
            let value = xval.readValue(type, cell)
            if (value == null) {
                return error(sheet, { ri: ri, ci: ci }, `not fit current type`)
            }

            row[name] = value
        }

        rows.push(row)
    }

    return { rows: rows }
}

const Code = {
      SpaceBegin: 'export namespace xlsx {'
    , ClassBegin: '\r\n    export class {NAME}Row {'
    , ClassLine : '\r\n        readonly {MEMB}: {TYPE} //{CMMT}'
    , ClassEnd  : '\r\n    }'
    , DataBegin : '\r\n    export class {NAME}Table {'
    + /*.......*/ '\r\n        rows: {NAME}Row[]'
    + /*.......*/ '\r\n    }'
    + /*.......*/ '\r\n    export const {NAME}TB: {NAME}Table = {'
    + /*.......*/ '\r\n        rows: ['
    , DataLine  : '\r\n            {LINE},'
    , DataEnd   : '\r\n        ]'
    + /*.......*/ '\r\n    }'
    + /*.......*/ '\r\n'
    , SpaceEnd  : '\r\n}'
    + /*.......*/ '\r\n'
}

class TsGenerator {

    _words = []

    constructor() {
        this._words.push(Code.SpaceBegin)
    }

    /**
     * @param {TableNorm} norm
     * @param {TableData} data
     */
    write(norm, data) {
        //class declaration.
        this._words.push(Code.ClassBegin.replace(/\{NAME\}/g, norm.name))

        for (let n = 0; n < norm.members.length; ++n) {
            if (norm.ignores[n]) {
                continue
            }

            let memb = norm.members[n]
            let type = norm.types[n]
            let cmmt = norm.comments[n]
            
            switch (type) {
                case xval.TypeEnum.Int      : type = 'number'; break
                case xval.TypeEnum.Float    : type = 'number'; break
                case xval.TypeEnum.String   : type = 'string'; break

                case xval.TypeEnum.IntArray : type = 'number[]'; break
                case xval.TypeEnum.FltArray : type = 'number[]'; break
                case xval.TypeEnum.StrArray : type = 'string[]'; break

                case xval.TypeEnum.IntIntMap: type = '{ [index: number]: number }'; break
                case xval.TypeEnum.IntFltMap: type = '{ [index: number]: number }'; break
                case xval.TypeEnum.IntStrMap: type = '{ [index: number]: string }'; break
                case xval.TypeEnum.StrIntMap: type = '{ [index: string]: number }'; break
                case xval.TypeEnum.StrFltMap: type = '{ [index: string]: number }'; break
                case xval.TypeEnum.StrStrMap: type = '{ [index: string]: string }'; break
            }

            this._words.push(Code.ClassLine.replace(/\{MEMB\}|\{TYPE\}|\{CMMT\}/g, (flag) => {
                return {
                    '{MEMB}': memb,
                    '{TYPE}': type,
                    '{CMMT}': cmmt,
                }[flag]
            }))
        }

        this._words.push(Code.ClassEnd)

        //data definition.
        this._words.push(Code.DataBegin.replace(/\{NAME\}/g, norm.name))

        for (let row of data.rows) {
            let line = JSON.stringify(row)
            this._words.push(Code.DataLine.replace(/\{LINE\}/g, line))
        }

        this._words.push(Code.DataEnd)
    }

    save() {
        this._words.push(Code.SpaceEnd)

        let code = this._words.join('')
        fs.writeFileSync(OutputTsFile, code)
    }
}

/**
 * @param  {?xlsx.XlsxSheet}         sheet
 * @param  {?{ri:number, ci:number}} coord
 * @param  {string}                  text
 * @return {null}
 */
function error(sheet, coord, text) {
    if (sheet && coord) {
        console.log('ERROR: '
            + `${sheet.filePath},${sheet.sheetName} `
            + `(${coord.ri + 1}, ${String.fromCharCode('A'.charCodeAt(0) + coord.ci)}): `
            + `${text}`
        )
    } else if (sheet) {
        console.log('ERROR: '
            + `${sheet.filePath},${sheet.sheetName}: `
            + `${text}`
        )
    } else {
        console.log(`ERROR: ${text}`)
    }
    return null
}

main()
