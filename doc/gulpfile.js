const {dest, src, parallel, series, watch: gulpwatch} = require('gulp');
const notify = require('gulp-notify');
const autoprefixer = require('autoprefixer');
const sass = require('gulp-sass')(require('sass'));
const uglify = require('gulp-uglify');
const concat = require('gulp-concat');
const postcss = require('gulp-postcss');
const del = require('del');
const sourcemaps = require('gulp-sourcemaps');
const stripImportExport = require('gulp-strip-import-export');
const browserSync = require('browser-sync').create();
const exec = require('child_process').exec;

const assets = 'templates/uno';

let isStrict = false;

function styles(done) {
    const output = 'compressed';

    src([`${assets}/vendor/*.css`])
        .pipe(dest(`${assets}/styles/`));

    src([`${assets}/**/*.scss`, `${assets}/**/*.sass`])
        .pipe(sourcemaps.init())
        .pipe(
            sass({includePaths: ['./node_modules/'], outputStyle: output}).on(
                'error',
                sass.logError
            )
        )
        .pipe(postcss([autoprefixer]))
        .pipe(sourcemaps.mapSources(function (sourcePath) {
            return '../' + sourcePath;
        }))
        .pipe(concat('main.css'))
        .pipe(sourcemaps.write('.'))
        .pipe(dest(`${assets}/styles/`))
        .pipe(notify({message: 'CSS complete'}));

    done();
}

function docfx(done) {
    exec('docfx docfx.json', (err, stdout, stderr) => {

        // This will print the docfx errors
        if (isStrict) {
            console.log(stdout);
            console.log(stderr);
            // This will stop the execution of the task on error
            // At the moment there is an error on every build
            // This a workaround
            if (err) {
                done(err);
                return;
            }
        }

        // Copy our custom files to override default template
        const fs = require('fs');
        const filesToCopy = [
            {
                source: `${assets}/styles/docfx.js`,
                destination: '_site/styles/docfx.js',
                critical: true
            },
            {
                source: `${assets}/styles/docfx.js.map`,
                destination: '_site/styles/docfx.js.map',
                critical: false
            },
            {
                source: `${assets}/styles/main.css`,
                destination: '_site/styles/main.css',
                critical: true
            },
            {
                source: `${assets}/styles/main.css.map`,
                destination: '_site/styles/main.css.map',
                critical: false
            },
            {
                source: `${assets}/styles/main.js`,
                destination: '_site/styles/main.js`,
                critical: true
            }
        ];

        let criticalCopyError = null;

        filesToCopy.forEach(file => {
            try {
                fs.copyFileSync(file.source, file.destination);
            } catch (copyErr) {
                const message = `Failed to copy "${file.source}" to "${file.destination}": ${copyErr.message}`;
                if (file.critical) {
                    console.error('Error:', message);
                    if (!criticalCopyError) {
                        criticalCopyError = copyErr;
                    }
                } else {
                    console.warn('Warning:', message);
                }
            }
        });

        if (criticalCopyError) {
            done(criticalCopyError);
            return;
        }
        done();
    });
}

function scripts(done) {
    src([`${assets}/main.js`])
        .pipe(sourcemaps.init())
        .pipe(uglify())
        .pipe(sourcemaps.write('.'))
        .pipe(stripImportExport())
        .pipe(dest(`${assets}/styles/`));

    src([`${assets}/vendor/*.js`])
        .pipe(dest(`${assets}/styles/`));

    src([`${assets}/**/*.js`,
        `!${assets}/styles/*.js`,
        `!${assets}/conceptual.html.primary.js`,
        `!${assets}/main.js`,
        `!${assets}/vendor/*.js`])
        .pipe(sourcemaps.init())
        .pipe(uglify())
        .pipe(concat('docfx.js'))
        .pipe(sourcemaps.write('.'))
        .pipe(dest(`${assets}/styles/`));

    done();
}

function watch() {
    // Watch .scss files
    gulpwatch([`${assets}/**/*.scss`, `${assets}/**/*.sass`], series([styles, docfx]))
        .on('change', browserSync.reload);

    // Watch docfx files
    gulpwatch([assets + '/**/*.tmpl', assets + '/**/*.tmpl.partial'], series([docfx]))
        .on('change', browserSync.reload);

    // Watch javascript files
    gulpwatch([`${assets}/**/*.js`, `!${assets}/styles/*.js`], series([scripts, docfx]))
        .on('change', browserSync.reload);
}

function serve() {
    browserSync.init({
        server: {
            baseDir: "_site"
        }
    });
}

async function clean(done) {

    await del([`${assets}/styles/**`,
        `!${assets}/styles`,
        `_site/styles/**`,
        `!_site/styles/`]);

    done();
}

function useStrict(done) {
    isStrict = true;
    done();
}

const build = series(clean, styles, scripts, docfx);
const run = parallel(serve, watch);

exports.build = build;

exports.default = series(build, run);

exports.clean = series(clean);

exports.debug = series(build, run);

exports.strict = series(useStrict, build, run);
