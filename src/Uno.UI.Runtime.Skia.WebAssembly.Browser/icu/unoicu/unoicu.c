#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unicode/utypes.h>
#include <unicode/udata.h>
#include <unicode/ubrk.h>

int loaded_icu_data = false;

char* uno_udata_setCommonData(const void* bytes) {
    UErrorCode errorCode = U_ZERO_ERROR;
    udata_setCommonData(bytes, &errorCode);
    return U_FAILURE(errorCode) ? u_errorName(errorCode) : NULL;
}

UBreakIterator* init_line_breaker(UChar* chars) {
    UErrorCode status = U_ZERO_ERROR;

    UBreakIterator *breaker = ubrk_open(UBRK_LINE, NULL, chars, -1, &status);
    if (U_FAILURE(status)) {
        fprintf(stderr, "Failed to create break iterator: %s\n", u_errorName(status));
        return NULL;
    }

    ubrk_first(breaker);
    return breaker;
}

int32_t next_line_breaking_opportunity(UBreakIterator *breaker) {
    int32_t next = ubrk_next(breaker);
    if (next == UBRK_DONE) {
        ubrk_close(breaker);
        return -1; // UBRK_DONE is -1 already, but this is done explicitly for clarity
    } else {
        return next;
    }
}

// TEST CODE: DO NOT COMPILE THIS IN
// int main(int argc, char **argv) {
//     UChar* text = u"Hello ragaa";
// 	UBreakIterator* breaker = init_line_breaker(text);
//     for (int next_line_break = 0;;next_line_break = next_line_breaking_opportunity(breaker))
//     {
//         printf("next_line_breaking_opportunity: %d\n", next_line_break);
//         if (next_line_break == -1) break;
//     }
//     return EXIT_SUCCESS;
// }
