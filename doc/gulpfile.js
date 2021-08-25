const gulp = require('gulp'),
    notify = require('gulp-notify'),
    autoprefixer = require('autoprefixer'),
    sass = require('gulp-sass'),
    uglify = require('gulp-uglify'),
    rename = require('gulp-rename'),
    postcss = require('gulp-postcss'),
    gulpif = require('gulp-if'),
    sassLint = require('gulp-sass-lint'),
    browserSync = require('browser-sync').create(),
    sourcemaps = require('gulp-sourcemaps'),
    exec = require('child_process').exec;

let debug = false;
const assets = 'templates/uno';

/**
 * Put relative path to your assets :
 * - Wordpress  : 'public/wp-content/themes/YOUR_THEME/assets';
 * - Symfony : 'public/assets/';
 */

gulp.task('styles', function () {
    const output = debug ? 'nested' : 'compressed';
    return gulp
        .src(assets + '/css/main.scss')
        .pipe(gulpif(debug, sourcemaps.init()))
        .pipe(gulpif(debug, sassLint()))
        .pipe(gulpif(debug, sassLint.format()))
        .pipe(gulpif(debug, sassLint.failOnError()))
        .pipe(
            sass({includePaths: ['./node_modules/'], outputStyle: output}).on(
                'error',
                sass.logError
            )
        )
        .pipe(postcss([autoprefixer]))
        .pipe(rename({suffix: '.min'}))
        .pipe(gulpif(debug, sourcemaps.write()))
        .pipe(gulp.dest(assets + '/css'))
        .pipe(notify({message: 'CSS complete'}));
});

gulp.task('scripts', function () {
    const output = debug ? 'nested' : 'compressed';

    return gulp.src([assets + 'public/assets/js/lib/*.js'])
        .pipe(concat('docfx.min.js'))
        .pipe(uglify())
        .pipe(gulp.dest('public/assets/js'));
});

gulp.task('watch', () => {
    gulp.watch([assets + '/css/*.scss', assets + '/css/*.sass'], gulp.series(['styles']));
});

gulp.task('default', gulp.series('styles', 'watch'), (done) => { done(); });

gulp.task('debug', function () {
    debug = true;
    gulp.start('styles');
});

gulp.task('serve', function () {
    browserSync.init({
        server: {
            baseDir: "./_site"
        },
        // host: " 172.20.8.240" replace by your current ip to test on another device.
    });
});

gulp.task('build', function (cb) {
    exec('docfx build', function (err, stdout, stderr) {
        console.log(stdout);
        console.log(stderr);
        cb(err);
    });
});

/**
 * Handle errors and displays them in console
 * @param error
 */
function swallowError(error) {
    console.log(error.toString());
    this.emit('end');
}
