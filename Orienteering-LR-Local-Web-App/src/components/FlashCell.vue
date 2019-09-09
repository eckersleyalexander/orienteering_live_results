<template>
	<td id="flashCell" :class="{ flash }">
		{{ displayValue }}
	</td>
</template>

<style>

	@keyframes flash {
		from { 
			color: white;
		}

		to {
			color: #E0E000;
		}
	}

	.flash {
		animation-duration: 1500ms;
		animation-name: flash;
		animation-iteration-count: 16;
		animation-direction: alternate;
	}

</style>

<script>

	export default {

		name: 'flash-cell',
		props: ['display-value', 'watch-value'],

		data() {
			return {
				flash: false,
			};
		},
  
		watch: {
			watchValue() {
				if (this.flash) {
					this.flash = false;
					this.$nextTick(() => {
						this.$el.offsetWidth;  // Force element to reflow
						this.flash = true;
					});
				}

				else {
					this.flash = true;
				}
			},
		}

	}

</script>