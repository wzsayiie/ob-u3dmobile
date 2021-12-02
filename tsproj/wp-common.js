const path = require('path')

module.exports = {
    devtool: 'inline-source-map',

    externals: {
        csharp: 'commonjs csharp',
	    puerts: 'commonjs puerts',

        //"source-map-support" depends on node.js modules "path" and "fs".
        //these modules will be injected on runtime.
        path: 'commonjs path',
        fs  : 'commonjs fs'  ,
    },
    output: {
        filename: 'manuscript.js',
        path: path.resolve(__dirname, '../u3dmobile/Assets/Bundle/manuscript'),
    },
    entry: './manuscript/main.ts',

    module: {
        rules: [
            {
                test  : /\.ts$/,
                loader: 'ts-loader',
            },
        ],
    },
    resolve: {
        extensions: ['.ts', '.js'],
    },
}
