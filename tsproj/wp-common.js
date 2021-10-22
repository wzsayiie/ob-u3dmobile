const path = require('path')

module.exports = {
    entry: './manuscript/main.ts',

    externals: {
        csharp: 'commonjs csharp',
	    puerts: 'commonjs puerts',
    },
    output: {
        filename: 'manuscript.js',
        path: path.resolve(__dirname, '../u3dmobile/Assets/Bundle/manuscript'),
    },

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
