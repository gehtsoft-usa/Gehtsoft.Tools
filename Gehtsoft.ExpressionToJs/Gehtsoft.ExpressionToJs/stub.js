function jsv_equal(a, b) { return a == b; }

function jsv_multiply(a, b) { return a * b; }

function jsv_divide(a, b) { return a / b; }

function jsv_notequal(a, b) { return a != b; }

function jsv_greater(a, b) { return a > b; }

function jsv_greaterorequal(a, b) { return a >= b; }

function jsv_and(a, b) { return a && b; }

function jsv_bwand(a, b) { return a & b; }

function jsv_plus(a, b) { return a + b; }

function jsv_minus(a, b) { return a - b; }

function jsv_unaryminus(a) { return ((-1) * a); }

function jsv_less(a, b) { return a < b; }

function jsv_modulus(a, b) { return a % b; }

function jsv_powerof(a, b) { return Math.pow(a, b); }

function jsv_lessorequal(a, b) { return a <= b; }

function jsv_isempty(obj) {
    if (obj == null)
        return true;
    if (!(typeof obj === 'string'))
        obj = obj.toString();
    return obj.length < 1;
}

function jsv_tostring(obj) {
    if (obj == null)
        return "";
    if (!(typeof obj === 'string'))
        obj = obj.toString();
    return obj;
}

function jsv_string2bool(s) {
    s = s.toString().toLowerCase();
    if (s === "true" || s === "yes" || s === "1" || s === "on")
        return true;
    if (s === "false" || s === "no" || s === "0" || s === "off")
        return false;
    return null;
}

function jsv_string2int(s) {
    return parseInt(s);
}

function jsv_string2n(s) {
    return parseFloat(s);
}

function jsv_trim(str) { return str.trim(); }

function jsv_lower(str) { return str.toLowerCase(); }

function jsv_upper(str) { return str.toUpperCase(); }

function jsv_concat(str1, str2) {
    return jsv_tostring(str1).concat(jsv_tostring(str2));
}

function jsv_match(pattern, str) {
    var result = str.match(pattern);
    return result != null && result.length > 0;
}

function jsv_isemptyorwhitespace(str) {
    return str == null || str.match(/^ *$/) != null;
}

function jsv_abs(a) { return Math.abs(a); }

function jsv_or(a, b) { return a || b; }

function jsv_bwor(a, b) { return a | b; }

function jsv_not(a) { return !a; }

function jsv_today() {
    return new Date();
}

function jsv_dayssince(date0, date1) {
    return (new Date(date0).getTime() - new Date(date1).getTime()) / 86400000;
}

function jsv_monthssince(d2, d1) {
    var months;
    d1 = new Date(d1);
    d2 = new Date(d2);
    months = (d2.getFullYear() - d1.getFullYear()) * 12;
    months -= d1.getMonth() + 1;
    months += d2.getMonth();
    if (d2.getDate() >= d1.getDate()) months++;
    return months <= 0 ? 0 : months;
}

function jsv_yearssince(date0, date1) {
    var d0 = new Date(date0);
    d0.setHours(12, 0, 0, 0);
    var d1 = new Date(date1);
    d1.setHours(12, 0, 0, 0);

    var negative = false;

    if (d0 > d1) {
        var t = d0;
        d0 = d1;
        d1 = t;
        negative = true;
    }

    var dY = d1.getFullYear() - d0.getFullYear();

    d0.setYear(d0.getFullYear() + dY);

    if (d0 > d1) {
        d0.setYear(d0.getFullYear() - 1);
        --dY;
    }

    return negative ? -dY : dY;
}

function jsv_adddays(date0, days) {
    return new Date(new Date(date0).getTime() + (days * 86400000));
}

function jsv_datediff(str2, str1) {
    var diff = (new Date(str2).valueOf() - new Date(str1).valueOf()) / (86400000);
    return parseFloat(diff.toFixed(1));
}

function jsv_length(value) {
    if (value === null || value === undefined) {
        return 0;
    }

    if (value["length"] === null)
        return 0;

    if (typeof value["length"] === "function") {
        return value.length();
    } else {
        return value.length;
    }
}

function jsv_ccn_valid(value) {
    var value = value.toString();

    if (value === null || value === undefined)
        return false;

    var checksum = 0;
    var evenDigit = false;

    for (var j = value.length - 1; j >= 0; j--) {
        var digit = value.charCodeAt(j);
        if (digit >= 0x30 && digit <= 0x39) {
            digit = digit - 0x30;

            if (evenDigit)
                digit = digit * 2;

            evenDigit = !evenDigit;

            while (digit > 0) {
                checksum += digit % 10;
                digit = Math.floor(digit / 10);
            }
        } else {
            if (digit === 0x20 || digit === 0x2d)
                continue;
            else
                return false;
        }
    }
    return checksum % 10 === 0;
}

function jsv_trunc(value) {
    if (value < 0)
        return Math.ceil(value);
    else
        return Math.floor(value);
}

function jsv_index(value, index) {
    if (typeof index !== 'string') {
        index = Math.floor(index);
    }

    if (typeof (value) === 'string') {
        return value.charCodeAt(index);
    } else {
        return value[index];
    }
}

function jsv_isUpperCase(value) {
    return (value >= 0x41 && value <= 0x5a) ||
        (value >= 0xc0 && value <= 0xde && value !== 0xd7) ||
        (value >= 0x410 && value <= 0x42f);
}

function jsv_isLowerCase(value) {
    return (value >= 0x61 && value <= 0x7a) ||
        (value >= 0xdf && value <= 0xff && value !== 0xf7) ||
        (value >= 0x430 && value <= 0x44f);
}

function jsv_isLetter(value) {
    return jsv_isUpperCase(value) || jsv_isLowerCase(value);
}

function jsv_isDigit(value) {
    return value >= 0x30 && value <= 0x39;
}

function jsv_isLetterOrDigit(value) {
    return jsv_isLetter(value) || jsv_isDigit(value);
}

function jsv_isWhiteSpace(value) {
    return (value === 0x20 || value === 0xa0 || value === 0x1680 || (value >= 0x2000 && value <= 0x200a) || value === 0x202f);
}

function jsv_isControl(value) {
    return (value >= 0x0 && value < 0x20) || value === 0x7f || (value >= 0x80 && value <= 0x9f);
}

function jsv_isPunctuation(value) {
    return (value >= 0x0021 && value <= 0x0023) || (value === 0x060C || value === 0x060D) || (value >= 0x1800 && value <= 0x180A) ||
        (value >= 0x3014 && value <= 0x301F) || (value >= 0x0025 && value <= 0x002A) || value === 0x061B || (value === 0x1944 || value === 0x1945) ||
        value === 0x3030 || (value >= 0x002C && value <= 0x002F) || (value === 0x061E || value === 0x061F) || (value === 0x19DE || value === 0x19DF) ||
        value === 0x303D || (value === 0x003A || value === 0x003B) || (value >= 0x066A && value <= 0x066D) || (value === 0x1A1E || value === 0x1A1F) ||
        value === 0x30A0 || (value === 0x003F || value === 0x0040) || value === 0x06D4 || (value >= 0x1B5A && value <= 0x1B60) || value === 0x30FB ||
        (value >= 0x005B && value <= 0x005D) || (value >= 0x0700 && value <= 0x070D) || (value >= 0x2010 && value <= 0x2027) || (value >= 0xA874 && value <= 0xA877) || value === 0x005F ||
        (value >= 0x07F7 && value <= 0x07F9) || (value >= 0x2030 && value <= 0x2043) || (value === 0xFD3E || value === 0xFD3F) || value === 0x007B || (value === 0x0964 || value === 0x0965) ||
        (value >= 0x2045 && value <= 0x2051) || (value >= 0xFE10 && value <= 0xFE19) || value === 0x007D || value === 0x0970 || (value >= 0x2053 && value <= 0x205E) ||
        (value >= 0xFE30 && value <= 0xFE52) || value === 0x00A1 || value === 0x0DF4 || (value === 0x207D || value === 0x207E) || (value >= 0xFE54 && value <= 0xFE61) || value === 0x00AB ||
        (value >= 0x0E4F && value <= 0x0E5B) || (value === 0x208D || value === 0x208E) || value === 0xFE63 || value === 0x00AD || (value >= 0x0F04 && value <= 0x0F12) || (value === 0x2329 || value === 0x232A) ||
        value === 0xFE68 || value === 0x00B7 || (value >= 0x0F3A && value <= 0x0F3D) || (value >= 0x2768 && value <= 0x2775) || (value === 0xFE6A || value === 0xFE6B) ||
        value === 0x00BB || value === 0x0F85 || (value >= 0x27C5 && value <= 0x27C6) || (value >= 0xFF01 && value <= 0xFF03) || value === 0x00BF ||
        (value === 0x0FD0 || value === 0x0FD1) || (value >= 0x27E6 && value <= 0x27EB) || (value >= 0xFF05 && value <= 0xFF0A) || value === 0x037E || (value >= 0x104A && value <= 0x104F) ||
        (value >= 0x2983 && value <= 0x2998) || (value >= 0xFF0C && value <= 0xFF0F) || value === 0x0387 || value === 0x10FB || (value >= 0x29D8 && value <= 0x29DB) || (value === 0xFF1A || value === 0xFF1B) ||
        (value >= 0x055A && value <= 0x055F) || (value >= 0x1361 && value <= 0x1368) || (value === 0x29FC || value === 0x29FD) || (value === 0xFF1F || value === 0xFF20) ||
        (value === 0x0589 || value === 0x058A) || (value === 0x166D || value === 0x166E) || (value >= 0x2CF9 && value <= 0x2CFC) || (value >= 0xFF3B && value <= 0xFF3D) ||
        value === 0x05BE || (value === 0x169B || value === 0x169C) || (value === 0x2CFE || value === 0x2CFF) || value === 0xFF3F || value === 0x05C0 ||
        (value >= 0x16EB && value <= 0x16ED) || (value >= 0x2E00 && value <= 0x2E17) || value === 0xFF5B || value === 0x05C3 || (value === 0x1735 || value === 0x1736) || (value === 0x2E1C || value === 0x2E1D) ||
        value === 0xFF5D || value === 0x05C6 || (value >= 0x17D4 && value <= 0x17D6) || (value >= 0x3001 && value <= 0x3003) || (value >= 0xFF5F && value <= 0xFF65) || (value === 0x05F3 || value === 0x05F4) ||
        (value >= 0x17D8 && value <= 0x17DA) || (value >= 0x3008 && value <= 0x3011);
}

function jsv_any(value, predicate) {
    for (var i = 0; i < jsv_length(value); i++) {
        if (predicate(jsv_index(value, i)))
            return true;
    }
    return false;
}

function jsv_all(value, predicate) {
    for (var i = 0; i < jsv_length(value); i++) {
        if (!predicate(jsv_index(value, i)))
            return false;
    }
    return true;
}

function jsv_count(value, predicate) {
    var count = 0;
    for (var i = 0; i < jsv_length(value); i++) {
        if (predicate == undefined || predicate(jsv_index(value, i)))
            count = count + 1;
    }
    return count;
}

function jsv_first(value, predicate, defaultValue) {
    for (var i = 0; i < jsv_length(value); i++) {
        var current = jsv_index(value, i);
        if (predicate(current))
            return current;
    }
    return defaultValue;
}

function jsv_last(value, predicate, defaultValue) {
    for (var i = jsv_length(value) - 1; i >= 0; i--) {
        var current = jsv_index(value, i);
        if (predicate(current))
            return current;
    }
    return defaultValue;
}

function jsv_sign(value) {
    if (value < 0)
        return -1.0;
    else if (value == 0)
        return 0.0;
    else
        return 1.0;
}

function jsv_round(value) {
    return Math.round(value);
}

function jsv_fractional(value) {
    return jsv_abs(value) - jsv_trunc(jsv_abs(value));
}