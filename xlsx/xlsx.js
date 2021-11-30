const fs    = require('fs')
const npath = require('path')
const read  = require('read-excel-file/node')

/**
 * @typedef  {Object}     XlsxSheet
 * @property {string}     filePath
 * @property {string}     sheetName
 * @property {string[][]} dataCells
 */

/**
 * @param {string}                        dir
 * @param {(sheets: XlsxSheet[]) => void} callback
 */
function readXlsx(dir, callback) {
    let files = collectFiles(dir)
    collectSheetRoutes(files, (routes) => {
        collectSheets(routes, callback)
    })
}

/**
 * @param  {string} dir
 * @return {string[]}
 */
 function collectFiles(dir) {
    let outFiles = []
    collectFilesInner(dir, outFiles)
    return outFiles
}

/**
 * @param {string}   dir
 * @param {string[]} outFiles
 */
function collectFilesInner(dir, outFiles) {
    let subitems = fs.readdirSync(dir)
    for (let item of subitems) {
        if (item.startsWith('.') || item.startsWith('~')) {
            //NOTE: ignore these files.
            //ms office and wps may create temporary files with the "xlsx" extension.
            continue
        }

        let path = npath.join(dir, item)
        let stat = fs.statSync(path)

        if (stat.isDirectory()) {
            collectFilesInner(path, outFiles)
        } else if (path.endsWith('.xlsx')) {
            outFiles.push(path)
        }
    }
}

/**
 * @typedef  {Object} SheetRoute
 * @property {string} filePath
 * @property {string} sheetName
 */

/**
 * @param {string[]}                       files
 * @param {(routes: SheetRoute[]) => void} callback
 */
function collectSheetRoutes(files, callback) {
    let outRoutes = []
    collectSheetRoutesInner(0, files, outRoutes, () => {
        callback(outRoutes)
    })
}

/**
 * @param {number}       startIndex
 * @param {string[]}     files
 * @param {SheetRoute[]} outRoutes
 * @param {() => void}   callback
 */
function collectSheetRoutesInner(startIndex, files, outRoutes, callback) {
    if (startIndex == files.length) {
        callback()
        return
    }

    let current = files[startIndex]
    read(current, { getSheets: true }).then((sheetNames) => {
        do {
            if (!sheetNames || sheetNames.length == 0) {
                break
            }

            for (let name of sheetNames) {
                let sheet = {
                    filePath : current  ,
                    sheetName: name.name,
                }
                outRoutes.push(sheet)
            }
        } while (false)

        collectSheetRoutesInner(startIndex + 1, files, outRoutes, callback)
    })
}

/**
 * @param {SheetRoute[]}                  routes
 * @param {(sheets: XlsxSheet[]) => void} callback
 */
function collectSheets(routes, callback) {
    let outSheets = []
    collectSheetsInner(0, routes, outSheets, () => {
        callback(outSheets)
    })
}

/**
 * @param {number}       startIndex
 * @param {SheetRoute[]} routes
 * @param {XlsxSheet[]}  outSheets
 * @param {() => void}   callback
 */
function collectSheetsInner(startIndex, routes, outSheets, callback) {
    if (startIndex == routes.length) {
        callback()
        return
    }

    let current = routes[startIndex]
    read(current.filePath, { sheet: current.sheetName }).then((cells) => {
        let stringCells = convertStrings(cells)
        ensureRectangle(stringCells)

        let sheet = {
            filePath : current.filePath ,
            sheetName: current.sheetName,
            dataCells: stringCells
        }
        outSheets.push(sheet)

        collectSheetsInner(startIndex + 1, routes, outSheets, callback)
    })
}

/**
 * @param  {Object[][]} src
 * @return {string[][]}
 */
function convertStrings(src) {
    let dst = []
    for (let srcRow of src) {
        let dstRow = []

        for (let cell of srcRow) {
            if (cell === undefined || cell === null) {
                dstRow.push('')
            } else if (typeof(cell) == 'string') {
                dstRow.push(cell)
            } else {
                dstRow.push(String(cell))
            }
        }

        dst.push(dstRow)
    }
    return dst
}

/**
 * @param {string[][]} cells
 */
function ensureRectangle(cells) {
    let width = 0
    for (let row of cells) {
        if (width < row.length) {
            width = row.length
        }
    }

    for (let row of cells) {
        for (let n = row.length; n < width; ++n) {
            row.push('')
        }
    }
}

module.exports = {
    readXlsx: readXlsx
}
