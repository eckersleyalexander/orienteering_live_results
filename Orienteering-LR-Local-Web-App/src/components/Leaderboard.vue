<template>

	<div v-if="pages[pageNum]" 
	class="columns resultsScreen"
	:style="{ paddingLeft: pageSidePadding + 'px', paddingRight: pageSidePadding + 'px', paddingTop: pageTopPadding + 'px', paddingBottom: pageBottomPadding + 'px' }"
	>
		<table
			v-for="(column, columnI) of pages[pageNum]"
			:key="columnI"
			class="column"
			:style="{ marginLeft: (columnI === 0 ? 0 : columnGap) + 'px' }"
		>

			<template
				v-for="(results, resultsI) of column.classes"
			>

				<tr
					:key="`${resultsI}-header`"
					class="headingRow"
					:style="{ height: headerRowHeight + 'px' }"
				>
					<th class="className" colspan="3"><span class="pillIcon" :style="{ backgroundColor: classColor(results.cls.clsName) }">{{ results.cls.clsName }} <span class="contText">{{ results.continued ? '(Cont...)' : '' }}</span><div class="classMetadata"><span class="classLength">{{ results.cls.length != null ? formatDistance(results.cls.length) + ' km' : '' }}</span> &#8226; <span class="classCourse">{{ results.cls.course }}</span></div></span></th>
					<th class="elapsedHeading" colspan="2">Total</th>
					<th class="splitHeading" colspan="3" v-for="(n, i) in results.cls.radiosCount" :key="n">Split {{ n }} count {{ results.cls.radiosCount }} -</th>
					<th
						v-if="results.cls.radiosCount < column.maxRadioCount"
						:colspan="(column.maxRadioCount - results.cls.radiosCount) * 3"
					/>
				</tr>

				<tr
					v-for="result of results.results"
					:key="`${resultsI}-result-${result.id}`"
					:style="{ height: rowHeight + 'px' }"
					v-bind:class="{ 'proceedToDownload':(result.status == 100) }"
				>

					<td class="col-overallRank" :style="{ width: colOverallRank + 'px' }">
						<font-awesome-icon v-if="result.startTime > 0 && result.status == 0 && statusZero(result) == 'running'" icon="running" class="pillIcon running" />
						<font-awesome-icon v-else-if="(result.status == 0 && statusZero(result) == 'pending') || (result.startTime <= 0 && result.status == 0)" icon="ellipsis-h" />
						<template v-else-if="result.status == 1"><span class="pillIcon finisher">{{ result.finishRank }}</span></template>
						<template v-else-if="result.status == 100"><span class="pillIcon finisher">{{ result.finishRank }}</span></template>
						<template v-else><span class="pillIcon nonfinisher">{{ statusToRank[result.status] }}</span></template>
					</td>

					<td class="col-competitor" :style="{ width: colCompetitor + 'px' }">{{ result.competitor }}</td>
					<td class="col-club" :style="{ width: colClub + 'px' }">{{ result.club }}</td>

					<td class="col-elapsedTime" :style="{ width: colElapsedTime + 'px' }">
						<template v-if="result.startTime > 0 && competitorStarted(result.startTime) == false"><span class="startTimeDisplay">{{ (result.startTime / 10) | formatStartTime }}</span></template>
						<template v-else-if="result.startTime > 0 && result.finishTime == null && result.status == 0">{{ (calculateElapsedTime(result.startTime) / 10) | formatAbsoluteTime }}</template>
						<template v-else>{{ result.finishTime }}</template>
					</td>

					<td class="col-elapsedDiff" :style="{ width: colElapsedDiff + 'px' }">
						<template v-if="competitorStarted(result.startTime) == false"><span class="startTimeDisplay">Start</span></template>
						<template v-else>{{ result.finishDiff }}</template>
					</td>

					<!-- we need to use i (index) rather than n (value) so we start at zero-->
					<template v-for="(n, i) in results.cls.radiosCount">

						<flash-cell :display-value="result.radios[i].time" :watch-value="result.radios[i].time" :key="result.id + '-' + result.radios[i].code + '-time'" class="col-radioTime" :style="{ width: colRadioTime + 'px' }"></flash-cell>

						<!-- if no radio punch then print no brackets-->
						<flash-cell v-if="result.radios[i].time == null" :display-value="null" :watch-value="result.radios[i].time" :key="result.id + '-' + result.radios[i].code + '-rank'" class="col-radioRank" :style="{ width: colRadioRank + 'px' }"></flash-cell>
						<flash-cell v-else :display-value="'(' + result.radios[i].rank + ')'" :watch-value="result.radios[i].time" :key="result.id + '-' + result.radios[i].code + '-rank'" class="col-radioRank" :style="{ width: colRadioRank + 'px' }"></flash-cell>

						<flash-cell :display-value="result.radios[i].diff" :watch-value="result.radios[i].time" :key="result.id + '-' + result.radios[i].code + '-diff'" class="col-radioDiff" :style="{ width: colRadioDiff + 'px' }"></flash-cell>

					</template>

					<td
						v-if="results.cls.radiosCount < column.maxRadioCount"
						:colspan="(column.maxRadioCount - results.cls.radiosCount) * 3"
					/>

				</tr>

			</template>

		</table>

		<div id="marquee" v-bind:class="{ 'marqueeHidden': resultsResponse.marquee.show == 0, 'marqueeShow': resultsResponse.marquee.show != 0 }">
			<marquee-text :repeat="10" :duration="parseInt(resultsResponse.marquee.duration)">{{ resultsResponse.marquee.text }}</marquee-text>
		</div>

	</div>

</template>

<style scoped>

    html {
        font-family: 'Futura';
    }

    /* roboto-100 - latin */
    @font-face {
        font-family: 'Roboto';
        font-style: normal;
        font-weight: 100;
        src: local('Roboto Thin'), local('Roboto-Thin'), url('/fonts/roboto-v20-latin-100.woff2') format('woff2'), /* Chrome 26+, Opera 23+, Firefox 39+ */
        url('/fonts/roboto-v20-latin-100.woff') format('woff'); /* Chrome 6+, Firefox 3.6+, IE 9+, Safari 5.1+ */
    }
    /* roboto-100italic - latin */
    @font-face {
        font-family: 'Roboto';
        font-style: italic;
        font-weight: 100;
        src: local('Roboto Thin Italic'), local('Roboto-ThinItalic'), url('/fonts/roboto-v20-latin-100italic.woff2') format('woff2'), /* Chrome 26+, Opera 23+, Firefox 39+ */
        url('/fonts/roboto-v20-latin-100italic.woff') format('woff'); /* Chrome 6+, Firefox 3.6+, IE 9+, Safari 5.1+ */
    }
    /* roboto-300 - latin */
    @font-face {
        font-family: 'Roboto';
        font-style: normal;
        font-weight: 300;
        src: local('Roboto Light'), local('Roboto-Light'), url('/fonts/roboto-v20-latin-300.woff2') format('woff2'), /* Chrome 26+, Opera 23+, Firefox 39+ */
        url('/fonts/roboto-v20-latin-300.woff') format('woff'); /* Chrome 6+, Firefox 3.6+, IE 9+, Safari 5.1+ */
    }
    /* roboto-300italic - latin */
    @font-face {
        font-family: 'Roboto';
        font-style: italic;
        font-weight: 300;
        src: local('Roboto Light Italic'), local('Roboto-LightItalic'), url('/fonts/roboto-v20-latin-300italic.woff2') format('woff2'), /* Chrome 26+, Opera 23+, Firefox 39+ */
        url('/fonts/roboto-v20-latin-300italic.woff') format('woff'); /* Chrome 6+, Firefox 3.6+, IE 9+, Safari 5.1+ */
    }
    /* roboto-regular - latin */
    @font-face {
        font-family: 'Roboto';
        font-style: normal;
        font-weight: 400;
        src: local('Roboto'), local('Roboto-Regular'), url('/fonts/roboto-v20-latin-regular.woff2') format('woff2'), /* Chrome 26+, Opera 23+, Firefox 39+ */
        url('/fonts/roboto-v20-latin-regular.woff') format('woff'); /* Chrome 6+, Firefox 3.6+, IE 9+, Safari 5.1+ */
    }
    /* roboto-italic - latin */
    @font-face {
        font-family: 'Roboto';
        font-style: italic;
        font-weight: 400;
        src: local('Roboto Italic'), local('Roboto-Italic'), url('/fonts/roboto-v20-latin-italic.woff2') format('woff2'), /* Chrome 26+, Opera 23+, Firefox 39+ */
        url('/fonts/roboto-v20-latin-italic.woff') format('woff'); /* Chrome 6+, Firefox 3.6+, IE 9+, Safari 5.1+ */
    }
    /* roboto-500 - latin */
    @font-face {
        font-family: 'Roboto';
        font-style: normal;
        font-weight: 500;
        src: local('Roboto Medium'), local('Roboto-Medium'), url('/fonts/roboto-v20-latin-500.woff2') format('woff2'), /* Chrome 26+, Opera 23+, Firefox 39+ */
        url('/fonts/roboto-v20-latin-500.woff') format('woff'); /* Chrome 6+, Firefox 3.6+, IE 9+, Safari 5.1+ */
    }
    /* roboto-500italic - latin */
    @font-face {
        font-family: 'Roboto';
        font-style: italic;
        font-weight: 500;
        src: local('Roboto Medium Italic'), local('Roboto-MediumItalic'), url('/fonts/roboto-v20-latin-500italic.woff2') format('woff2'), /* Chrome 26+, Opera 23+, Firefox 39+ */
        url('/fonts/roboto-v20-latin-500italic.woff') format('woff'); /* Chrome 6+, Firefox 3.6+, IE 9+, Safari 5.1+ */
    }
    /* roboto-700 - latin */
    @font-face {
        font-family: 'Roboto';
        font-style: normal;
        font-weight: 700;
        src: local('Roboto Bold'), local('Roboto-Bold'), url('/fonts/roboto-v20-latin-700.woff2') format('woff2'), /* Chrome 26+, Opera 23+, Firefox 39+ */
        url('/fonts/roboto-v20-latin-700.woff') format('woff'); /* Chrome 6+, Firefox 3.6+, IE 9+, Safari 5.1+ */
    }
    /* roboto-700italic - latin */
    @font-face {
        font-family: 'Roboto';
        font-style: italic;
        font-weight: 700;
        src: local('Roboto Bold Italic'), local('Roboto-BoldItalic'), url('/fonts/roboto-v20-latin-700italic.woff2') format('woff2'), /* Chrome 26+, Opera 23+, Firefox 39+ */
        url('/fonts/roboto-v20-latin-700italic.woff') format('woff'); /* Chrome 6+, Firefox 3.6+, IE 9+, Safari 5.1+ */
    }
    /* roboto-900 - latin */
    @font-face {
        font-family: 'Roboto';
        font-style: normal;
        font-weight: 900;
        src: local('Roboto Black'), local('Roboto-Black'), url('/fonts/roboto-v20-latin-900.woff2') format('woff2'), /* Chrome 26+, Opera 23+, Firefox 39+ */
        url('/fonts/roboto-v20-latin-900.woff') format('woff'); /* Chrome 6+, Firefox 3.6+, IE 9+, Safari 5.1+ */
    }
    /* roboto-900italic - latin */
    @font-face {
        font-family: 'Roboto';
        font-style: italic;
        font-weight: 900;
        src: local('Roboto Black Italic'), local('Roboto-BlackItalic'), url('/fonts/roboto-v20-latin-900italic.woff2') format('woff2'), /* Chrome 26+, Opera 23+, Firefox 39+ */
        url('/fonts/roboto-v20-latin-900italic.woff') format('woff'); /* Chrome 6+, Firefox 3.6+, IE 9+, Safari 5.1+ */
    }



div.resultsScreen {
	background-color: #333;
	color: #eee;
	font-family: Roboto;
	margin: 0;
}

table {
	border-collapse: collapse;
	white-space: nowrap;
	table-layout: fixed;
	overflow: hidden;
}

table tr td {
	/*border: 1px solid #777;*/
	vertical-align: bottom;
}

td, th {
	padding: 0 3px;
	box-sizing: border-box;
	position: relative;
}

th {
	border-bottom: 1px solid #777;
	vertical-align: bottom;
	padding-bottom: 10px;
}

@keyframes proceedToDownload {
	0% { 
		opacity: 0.1;
	}

	15% {
		opacity: 1;
	}

	25% {
		opacity: 1;
	}

	40% {
		opacity: 0.1;
	}

	100% {
		opacity: 0.1;
	}

}

tr.proceedToDownload td:first-child:after {
	position: absolute;
	z-index: 1;
	display: block;
	text-align: left;
	left: 0;
	top: 0;
	width: 1000px;
	height: 24px;
	padding-top: 5px;
	margin-left: -50px;
	content:"UNOFFICIAL • PROCEED TO DOWNLOAD • UNOFFICIAL • PROCEED TO DOWNLOAD • UNOFFICIAL • PROCEED TO DOWNLOAD";
	font-size: 17px;
	background: repeating-linear-gradient(
		45deg,
		#EF9F01,
		#EF9F01 10px,
		#FEBB34 10px,
		#FEBB34 20px
	);
	color: white;
	opacity: 0.1;
	animation-duration: 30000ms;
	animation-name: proceedToDownload;
	animation-iteration-count: infinite;
}


tr:nth-child(even){
	background-color: #444;
}

tr:nth-child(odd){
	background-color: #333;
}

th.className {
	text-align: left;
	font-size: 24px;
	text-transform: uppercase;
	padding-top: 10px;
	padding-left: 10px;
	font-weight: 500;
}

tr.headingRow th.className span.pillIcon {
	border-radius: 5px;
	display: inline-block;
	padding: 4px 16px 3px 16px;
	width: 225px;
}

th.className .classMetadata {
	margin-top: 5px;
	font-size: 14px;
	font-weight: 300;
}

th.className .classLength {
	margin-right: 5px;
}

th.className .classCourse {
	margin-left: 5px;
}

th.className .contText {
	font-size: 16px;
}

th.elapsedHeading {
	font-size: 14px;
	text-align: center;
}

th.splitHeading {
	font-size: 14px;
	text-align: center;
}

td {
	border-bottom: 1px solid #777;
}

td.col-overallRank {
	text-align: center;
	font-size: 13px;
	vertical-align: middle;
}

td.col-overallRank .pillIcon {
	border: none;
	padding: 2px 5px;
	color: white;
	text-align: center;
	text-decoration: none;
	display: inline-block;
	margin: 4px 2px;
	border-radius: 18px;
}

td.col-overallRank .pillIcon.finisher {
	background-color: #32a852;
}

td.col-overallRank .pillIcon.running {
	padding: 2px 7px;
	background-color: #d18400;
}

tr.proceedToDownload td.col-overallRank .pillIcon {
	background-color: #d18400;
}

td.col-overallRank .pillIcon.nonfinisher {
	background-color: #8c1414;
}

td.col-competitor {
	max-width: 180px;
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
	vertical-align: middle;
	font-weight: 500;
}

td.col-club {
	vertical-align: middle;
}

td.col-elapsedTime {
	text-align: right;
	vertical-align: middle;
}

td.col-elapsedTime .startTimeDisplay {
	color: #d18400;
	font-weight: 300;
}

td.col-elapsedDiff {
	text-align: left;
	font-size: 12px;
	vertical-align: middle;
	padding-top: 2px;
}

td.col-elapsedDiff .startTimeDisplay {
	color: #d18400;
	font-weight: 300;
}

td.col-radioTime {
	text-align: right;
	vertical-align: middle;
}

td.col-radioRank {
	font-size: 12px;
	vertical-align: middle;
	padding-top: 2px;
}

td.col-radioDiff {
	text-align: left;
	font-size: 12px;
	vertical-align: middle;
	padding-top: 2px;
}

.columns {
	display: flex;
}

.column {
	flex: 0 0 auto;
}

#marquee {
	background-color: black;
	height: 46px;
	width: 100%;
	position: fixed;
	bottom: 0;
	left: 0;
	padding-top: 8px;
	text-transform: uppercase;
	font-size: 32px;
}

#marquee .marquee-text-text {
	padding-right: 150px;
	white-space: pre-wrap;
}

@keyframes hideMarquee {
	0% { 
		bottom: 0;
	}

	100% {
		bottom: -54px;
	}

}

#marquee.marqueeHidden {
	/*display: none;*/
	animation-duration: 1000ms;
	animation-name: hideMarquee;
	animation-iteration-count: 1;
	bottom: -54px;
}

@keyframes showMarquee {
	0% { 
		bottom: -54px;
	}

	100% {
		bottom: 0px;
	}

}

#marquee.marqueeShow {
	/*display: none;*/
	animation-duration: 1000ms;
	animation-name: showMarquee;
	animation-iteration-count: 1;
	bottom: 0px;
}

</style>

<script>

	import FlashCell from '@/components/FlashCell.vue'

	export default {

		data() {
			return {
				now: new Date(),
				pageNum: parseInt(this.$route.params.page) || 0,
				resultsResponse: null,

				windowWidth: 0,
				windowHeight: 0,
				rowHeight: 30,
				headerRowHeight: 80,
				columnGap: 20,
				pageSidePadding: 10,
				pageTopPadding: 0,
				pageBottomPadding: 60,

				// Store the zoom level of the browser window
				zoomChrome: 1,

				// Column widths
				colOverallRank: 50,
				colCompetitor: 180,
				colClub: 50,
				colElapsedTime: 65,
				colElapsedDiff: 55,
				colRadioTime: 65,
				colRadioRank: 30,
				colRadioDiff: 47,

				statusToRank: {
					3: 'MP',
					4: 'DNF',
					5: 'DQ',
					6: 'OT',
					20: 'DNS',
					21: 'CNL',
					99: 'NP',
				}
			}
		},

		computed: {
			// Splits the data into "pages" such that all of the data within each page
			// will fit as efficiently as possible onto the page
			pages() {
				const {
					resultsResponse,
					windowHeight,
					rowHeight,
					headerRowHeight,
					columnGap,
					colOverallRank,
					colCompetitor,
					colClub,
					colElapsedTime,
					colElapsedDiff,
					colRadioTime,
					colRadioRank,
					colRadioDiff,
				} = this
				
				const windowWidth = this.windowWidth - this.pageSidePadding * 2

				if (!resultsResponse) {
					return []
				}

				const pages = []
				let page = null
				let column = null
				let results = null
				let lastCls = null
				let cls = null
				let tableWidth = 0
				let columnWidth = 0
				let columnHeight = 0
				let overallWidth = 0

				const tableBaseWidth = colOverallRank + colCompetitor + colClub + colElapsedTime + colElapsedDiff
				const tableRadioWidth = colRadioTime + colRadioRank + colRadioDiff

				const save = () => {
					// This is all the data we need in order to restore the state
					return {
						pagesLength: pages.length,
						pageLength: page && page.length,
						columnLength: column && column.classes.length,
						lastCls,
						tableWidth,
						columnWidth,
						columnHeight,
						overallWidth,
					}
				}

				const rollback = state => {
					({
						lastCls,
						tableWidth,
						columnWidth,
						columnHeight,
						overallWidth,
					} = state)

					pages.splice(state.pagesLength)
					page = pages[pages.length - 1]

					if (page) {
						page.splice(state.pageLength)
						column = page[page.length - 1]

						if (column) {
							column.classes.splice(state.columnLength)
							results = column.classes[column.classes.length - 1]
						}
					}
				}

				const fit = height => {
					const additionalWidth = Math.max(0, tableWidth - columnWidth)
					const overflowH = overallWidth + additionalWidth > windowWidth
					const overflowV = columnHeight + height + this.pageTopPadding + this.pageBottomPadding > windowHeight

					// Do we need another page?
					if (!page || (overflowH && page.length > 1)) {
						page = []
						pages.push(page)
						column = null
						results = null
						columnWidth = 0
						columnHeight = 0
						overallWidth = 0
						fit(height)
						return
					}

					// Do we need another column?
					if (!column || overflowV) {
						column = {
							maxRadioCount: 0,
							classes: [],
						}
						page.push(column)
						results = null
						columnWidth = tableWidth
						columnHeight = 0
						overallWidth += tableWidth + (page.length === 1 ? 0 : columnGap)
						fit(height)
						return
					}

					// Do we need another results section?
					if (!results) {
						results = []
						column.maxRadioCount = Math.max(column.maxRadioCount, cls.radioCount)
						column.classes.push({
							cls,
							results,
							continued: lastCls === cls,
						})
						lastCls = cls
						columnHeight += headerRowHeight
						fit(height)
						return
					}

					columnWidth += additionalWidth
					columnHeight += height
				}

				for (cls of resultsResponse.cmpResults) {
					tableWidth = tableBaseWidth + tableRadioWidth * cls.radioCount
					results = null
					
					// Save the current state in case we need to rollback
					const state = save()

					// Are we at the start of a page?
					const atStartOfPage = !page || !page.length || !page[0].classes.length
					const pagesLengthBefore = pages.length

					// Fit the class
					for (const result of cls.clsResults) {
						fit(rowHeight)
						results.push(result)
					}
					
					// Was the class split across multiple pages?
					const classSpansMultiplePages = pages.length > pagesLengthBefore

					if (classSpansMultiplePages && !atStartOfPage) {
						// Rollback
						rollback(state)

						// Fit the class on a new page
						page = null
						column = null
						results = null
						
						for (const result of cls.clsResults) {
							fit(rowHeight)
							results.push(result)
						}
					}
				}
				
				// Trim away empty columns/sections/etc
				for (let i = pages.length - 1; i >= 0; i--) {
					const page = pages[i]

					for (let j = page.length - 1; j >= 0; j--) {
						const column = page[j]

						for (let k = column.classes.length - 1; k >= 0; k--) {
							const section = column.classes[k]

							if (!section.results.length) {
								column.classes.splice(k, 1)
							}
						}

						if (!column.classes.length) {
							page.splice(j, 1)
						}
					}

					if (!page.length) {
						pages.splice(i, 1)
					}
				}
				return pages
			},
		},

		filters: {
			formatAbsoluteTime: function(t) {
				
				if (t) {

					// This code does hh:mm:ss for > 1 hour and mm:ss for < 1 hour

					/*
					var h, m, s;

					if (t > 3600) {
						h = Math.floor(t/3600).toString();
						m = Math.floor((t/60)%60).toString().padStart(2, '0');
						s = Math.floor(t%60).toString().padStart(2, '0');
						return `${h}:${m}:${s}`;
					}

					else {
						if (t >= 600 ) {
							m = Math.floor((t/60)%60).toString().padStart(2, '0');
						}
						else {
							m = Math.floor((t/60)%60).toString().padStart(1, '0');
						}
						s = Math.floor(t%60).toString().padStart(2, '0');
						return `${m}:${s}`;
					}
					*/

					// This code does mmm:ss for everyone
					var m, s;

					m = Math.floor(t/60).toString();
					s = Math.floor(t%60).toString().padStart(2, '0');
					return `${m}:${s}`;

				}

				return null;

			},

			formatStartTime: function(t) {

				var h, m, s;

				if (t > 3600) {
					h = Math.floor(t/3600).toString();
					m = Math.floor((t/60)%60).toString().padStart(2, '0');
					s = Math.floor(t%60).toString().padStart(2, '0');
					return `${h}:${m}:${s}`;
				}

			},

		},

		created () {
			window.addEventListener('resize', () => this.updateWindowSize())
			this.updateWindowSize()
			this.refreshResults()

			setInterval(() => this.now = new Date(), 1000)

			const updateLoop = () => {
				const nowMs = +new Date()
				const updateIntervalMs = 5000;
				const delay = Math.floor(nowMs / 1000) * 1000 - nowMs + updateIntervalMs

				setTimeout(() => {
					this.refreshResults()
					updateLoop()

				}, delay)
			}

			updateLoop()
		},

		components: {
			FlashCell
		},

		methods: {

			updateWindowSize() {
				this.windowWidth = window.innerWidth
				this.windowHeight = window.innerHeight

				// Update the browser window zoom level
				this.zoomChrome = Math.round(((window.outerWidth) / window.innerWidth) * 100) / 100;
				// console.log(this.zoomChrome);

				this.columnGap = 20 * this.zoomChrome
				this.colOverallRank = 50 * this.zoomChrome
				this.colCompetitor = 180 * this.zoomChrome
				this.colClub = 50 * this.zoomChrome
				this.colElapsedTime = 65 * this.zoomChrome
				this.colElapsedDiff = 55 * this.zoomChrome
				this.colRadioTime = 65 * this.zoomChrome
				this.colRadioRank = 30 * this.zoomChrome
				this.colRadioDiff = 47 * this.zoomChrome

			},

			async refreshResults () {
				const result = await fetch("http://localhost:9696/api/leaderboard")
				this.resultsResponse = await result.json() 
			},

			// Calculates the current elapsed time for a competitor, based on their startTime
			calculateElapsedTime(startTime) {

				// Time of day in 10ths of seconds
				const { now } = this;
				const currentTimeSecs = (now.getSeconds() + (60 * now.getMinutes()) + (60 * 60 * now.getHours())) * 10;

				// Calculate elapsed running time
				const elapsedRunningTime = currentTimeSecs - startTime;

				// Check that it's positive
				if (elapsedRunningTime >= 0) {

					// Return the result
					return elapsedRunningTime;

				}

				// Otherwise, return null
				return null;

			},

			// Returns whether a competitor has started yet or not
			competitorStarted(startTime) {

				// Number of seconds since midnight
				const { now } = this;
				const currentTimeSecs = (now.getSeconds() + (60 * now.getMinutes()) + (60 * 60 * now.getHours())) * 10;

				// Calculate elapsed running time
				const elapsedRunningTime = currentTimeSecs - startTime;

				// Check that it's positive
				if (elapsedRunningTime > 0) {

					// Return true
					return true;

				}

				// Otherwise, return false
				return false;

			},

			// Determines when the status is 0 whether the competitor is started or not yet started, and returns the relevant icon
			statusZero(resultObject) {

				// Calculate their elapsed time
				var elapsedRunningTime = this.calculateElapsedTime(resultObject.startTime);

				// They have not started
				if (elapsedRunningTime == null) {

					// Set the icon to three dots (pending...)
					return 'pending';

				}

				// They have started...
				else {

					// Set the icon to a running man
					return 'running';

				}

			},

			// Displays the distance info (if available) for a particular radio
			formatDistance(d) {

				// Convert the distance in meters into km for display, rounded to 1dp
				var distanceInKm = parseFloat(d / 1000).toFixed(1);

				// Return the distance
				return distanceInKm;

			},

			// Returns a colour for a given string, based upon the string's hash
			classColor(str) {

				var hash = 0;

				if (str.length === 0) return hash;

				for (var i = 0; i < str.length; i++) {
					hash = str.charCodeAt(i) + ((hash << 5) - hash);
					hash = hash & hash;
				}

				const h = Math.abs(hash) % 360;
				const s = Math.abs(hash) % 60 + 40;
				const l = (Math.abs(hash) % 25) + 20;

				return `hsl(${h}, ${s}%, ${l}%)`;

			},

		}

	}

</script>