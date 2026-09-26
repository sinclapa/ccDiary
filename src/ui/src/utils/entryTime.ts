import dayjs, { type Dayjs } from 'dayjs'
import utc from 'dayjs/plugin/utc'

dayjs.extend(utc)

/**
 * A diary entry's date is wall-clock time: the time the diarist wrote, in the place they were
 * standing. 13:00 in Kent and 13:00 in Kenya are both stored as 13:00 and both read back as
 * 13:00, because the entry records what the clock said, not an instant the whole world shares.
 *
 * Rendering such a value in the viewer's own zone would shift it — a diary written in 1918 was
 * showing 01:00 for entries stored at midnight, because the viewer happened to be in British
 * summer time. So entries are always read in UTC, which is where they were stored.
 */
export function entryTime (date: Date | string): Dayjs {
  return dayjs.utc(date)
}

/**
 * The entry date for a moment on the viewer's own clock: its local date and time, stored as the
 * wall clock. A new entry made at 14:05 in Kenya, or for a day picked in a calendar, keeps
 * 14:05 and that day, whichever zone the browser is in.
 */
export function toEntryDate (local: Date): Date {
  return new Date(Date.UTC(
    local.getFullYear(), local.getMonth(), local.getDate(),
    local.getHours(), local.getMinutes(), local.getSeconds(),
  ))
}

/**
 * A local Date that shows an entry's wall-clock date and time, for date and time pickers, which
 * work in the viewer's zone. The inverse of {@link toEntryDate}.
 */
export function fromEntryDate (entry: Date | string): Date {
  const d = entryTime(entry)
  return new Date(d.year(), d.month(), d.date(), d.hour(), d.minute(), d.second())
}

/**
 * The offset sent to the API when asking for a day's entries. Zero for the same reason: a day
 * runs from 00:00 to 24:00 on the diarist's clock, so windowing it by the viewer's offset would
 * push a late-evening entry into tomorrow and an early-morning one into yesterday.
 */
export const ENTRY_UTC_OFFSET_MINUTES = 0
